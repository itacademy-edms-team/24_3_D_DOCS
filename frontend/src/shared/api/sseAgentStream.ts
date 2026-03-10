import type { AgentResponseDTO, AgentStepDTO } from './AIAPI'; // type-only: avoids runtime cycle with AIAPI importing this module

const devLog = (...args: unknown[]) => {
	if (import.meta.env.DEV) {
		console.log(...args);
	}
};

/**
 * Читает SSE-поток от POST /api/ai/agent до события complete или error.
 */
export async function readAgentSseStream(
	reader: ReadableStreamDefaultReader<Uint8Array>,
	onStep?: (step: AgentStepDTO) => void
): Promise<AgentResponseDTO> {
	const decoder = new TextDecoder();
	let buffer = '';
	let finalResponse: AgentResponseDTO | null = null;

	while (true) {
		const { done, value } = await reader.read();

		if (done) {
			if (finalResponse) return finalResponse;
			throw new Error('Stream ended without complete event');
		}

		buffer += decoder.decode(value, { stream: true });

		const events = buffer.split('\n\n');
		buffer = events.pop() || '';

		for (const eventBlock of events) {
			if (!eventBlock.trim() || eventBlock.trim().startsWith(':')) continue;

			let eventType = '';
			let dataStr = '';

			for (const line of eventBlock.split('\n')) {
				const trimmedLine = line.trim();
				if (trimmedLine.startsWith('event: ')) {
					eventType = trimmedLine.substring(7).trim();
				} else if (trimmedLine.startsWith('data: ')) {
					const dataLine = trimmedLine.substring(6);
					dataStr = dataStr ? `${dataStr}\n${dataLine}` : dataLine;
				}
			}

			if (!dataStr) continue;

			let data: Record<string, unknown>;
			try {
				data = JSON.parse(dataStr) as Record<string, unknown>;
			} catch {
				if (import.meta.env.DEV) {
					console.warn('SSE: пропуск не-JSON блока', dataStr.substring(0, 120));
				}
				continue;
			}

			if (!eventType) {
				if (data.stepNumber !== undefined) eventType = 'step';
				else if (data.isComplete !== undefined) eventType = 'complete';
				else if (data.message) eventType = 'error';
			}

			if (eventType === 'step' || data.stepNumber !== undefined) {
				const step = data as unknown as AgentStepDTO;
				devLog('SSE step', step.stepNumber);
				onStep?.(step);
			} else if (eventType === 'complete' || data.isComplete !== undefined) {
				finalResponse = data as unknown as AgentResponseDTO;
				devLog('SSE complete');
			} else if (eventType === 'error' || data.message) {
				const errorMessage =
					data.details && data.message !== data.details
						? `${data.message || 'Ошибка'}: ${data.details}`
						: (data.message as string) || (data.details as string) || 'Unknown error';
				throw new Error(errorMessage);
			}
		}
	}
}
