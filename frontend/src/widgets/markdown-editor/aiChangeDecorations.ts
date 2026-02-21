import {
	RangeSetBuilder,
	StateEffect,
	StateField,
	type Extension,
} from '@codemirror/state';
import {
	Decoration,
	type DecorationSet,
	EditorView,
	WidgetType,
} from '@codemirror/view';
import type { Text } from '@codemirror/state';
import type { DocumentEntityChangeDTO } from '@/shared/api/AIAPI';

type ChangeHandler = (changeId: string) => void;

interface AiDecorationsState {
	changes: DocumentEntityChangeDTO[];
	decorations: DecorationSet;
}

export const setAiChangesEffect = StateEffect.define<DocumentEntityChangeDTO[]>();

class AiChangeWidget extends WidgetType {
	constructor(
		private readonly change: DocumentEntityChangeDTO,
		private readonly onAccept: ChangeHandler,
		private readonly onUndo: ChangeHandler
	) {
		super();
	}

	eq(other: AiChangeWidget): boolean {
		return (
			this.change.changeId === other.change.changeId &&
			this.change.changeType === other.change.changeType &&
			this.change.content === other.change.content &&
			this.change.entityType === other.change.entityType
		);
	}

	toDOM(): HTMLElement {
		const wrapper = document.createElement('div');
		wrapper.className = `cm-ai-change-widget cm-ai-change-widget--${this.change.changeType}`;

		const header = document.createElement('div');
		header.className = 'cm-ai-change-widget__header';
		header.textContent =
			this.change.changeType === 'insert'
				? `Добавление: ${this.change.entityType}`
				: `Удаление: ${this.change.entityType}`;

		const actions = document.createElement('div');
		actions.className = 'cm-ai-change-widget__actions';

		const acceptButton = document.createElement('button');
		acceptButton.type = 'button';
		acceptButton.className = 'cm-ai-change-widget__btn cm-ai-change-widget__btn--accept';
		acceptButton.textContent = 'Accept';
		acceptButton.addEventListener('click', (event) => {
			event.preventDefault();
			event.stopPropagation();
			this.onAccept(this.change.changeId);
		});

		const undoButton = document.createElement('button');
		undoButton.type = 'button';
		undoButton.className = 'cm-ai-change-widget__btn cm-ai-change-widget__btn--undo';
		undoButton.textContent = 'Undo';
		undoButton.addEventListener('click', (event) => {
			event.preventDefault();
			event.stopPropagation();
			this.onUndo(this.change.changeId);
		});

		actions.append(acceptButton, undoButton);
		header.appendChild(actions);
		wrapper.appendChild(header);

		if (this.change.changeType === 'insert') {
			const contentBlock = document.createElement('pre');
			contentBlock.className = 'cm-ai-change-widget__content';
			contentBlock.textContent = this.change.content;
			wrapper.appendChild(contentBlock);
		}

		return wrapper;
	}

	ignoreEvent(): boolean {
		return false;
	}
}

const clampLine = (line: number, min: number, max: number): number => {
	return Math.max(min, Math.min(line, max));
};

const getInsertPosition = (doc: Text, insertAfterLine: number): number => {
	if (insertAfterLine <= 0) {
		return 0;
	}

	const lineNumber = clampLine(insertAfterLine, 1, doc.lines);
	const line = doc.line(lineNumber);
	if (lineNumber >= doc.lines) {
		return doc.length;
	}

	return Math.min(doc.length, line.to + 1);
};

const buildDecorations = (
	doc: Text,
	changes: DocumentEntityChangeDTO[],
	onAccept: ChangeHandler,
	onUndo: ChangeHandler
): DecorationSet => {
	const ranges: Array<{ from: number; to?: number; deco: Decoration }> = [];

	for (const change of changes) {
		if (change.changeType === 'insert') {
			const insertPos = getInsertPosition(doc, change.startLine);
			ranges.push({
				from: insertPos,
				deco: Decoration.widget({
					block: true,
					side: 1,
					widget: new AiChangeWidget(change, onAccept, onUndo),
				}),
			});
			continue;
		}

		const startLine = clampLine(change.startLine, 1, doc.lines);
		const rawEndLine = change.endLine ?? change.startLine;
		const endLine = clampLine(Math.max(startLine, rawEndLine), startLine, doc.lines);
		const from = doc.line(startLine).from;
		const to = doc.line(endLine).to;

		ranges.push({
			from,
			to,
			deco: Decoration.mark({ class: 'cm-ai-change-delete' }),
		});

		ranges.push({
			from: to,
			deco: Decoration.widget({
				block: true,
				side: 1,
				widget: new AiChangeWidget(change, onAccept, onUndo),
			}),
		});
	}

	ranges.sort((a, b) => a.from - b.from || (a.to ?? a.from) - (b.to ?? b.from));

	const rangeBuilder = new RangeSetBuilder<Decoration>();
	for (const range of ranges) {
		rangeBuilder.add(range.from, range.to ?? range.from, range.deco);
	}

	return rangeBuilder.finish();
};

export const createAiChangeExtensions = (
	onAccept: ChangeHandler,
	onUndo: ChangeHandler
): Extension => {
	return StateField.define<AiDecorationsState>({
		create: () => ({
			changes: [],
			decorations: Decoration.none,
		}),
		update: (value, transaction) => {
			let nextChanges = value.changes;

			for (const effect of transaction.effects) {
				if (effect.is(setAiChangesEffect)) {
					nextChanges = effect.value;
				}
			}

			if (
				transaction.docChanged ||
				transaction.effects.some((effect) => effect.is(setAiChangesEffect))
			) {
				return {
					changes: nextChanges,
					decorations: buildDecorations(transaction.state.doc, nextChanges, onAccept, onUndo),
				};
			}

			return {
				changes: nextChanges,
				decorations: value.decorations.map(transaction.changes),
			};
		},
		provide: (field) =>
			EditorView.decorations.from(field, (state) => state.decorations),
	});
};
