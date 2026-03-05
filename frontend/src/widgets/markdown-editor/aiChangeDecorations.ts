import {
	RangeSetBuilder,
	StateEffect,
	StateField,
	type Text,
	type Extension,
} from '@codemirror/state';
import {
	Decoration,
	type DecorationSet,
	EditorView,
	WidgetType,
} from '@codemirror/view';
import type { DocumentAiChange } from '@/entities/document/types';

type ChangeHandler = (changeId: string) => void;

interface AiChangeGroup {
	referenceChangeId: string;
	kind: 'insert' | 'delete' | 'replace';
	anchorLine: number;
	deleteStartLine?: number;
	deleteEndLine?: number;
	insertContent?: string;
}

export const refreshAiChangeDecorationsEffect = StateEffect.define<null>();

function groupChanges(changes: DocumentAiChange[]): AiChangeGroup[] {
	const grouped = new Map<string, DocumentAiChange[]>();

	for (const change of changes) {
		const key = change.groupId ?? change.changeId;
		const items = grouped.get(key) ?? [];
		items.push(change);
		grouped.set(key, items);
	}

	return Array.from(grouped.values())
		.map((items) => {
			const sorted = [...items].sort((a, b) => (a.order ?? 0) - (b.order ?? 0));
			const inserts = sorted.filter((item) => item.changeType === 'insert');
			const deletes = sorted.filter((item) => item.changeType === 'delete');

			const insertContent = inserts.length > 0
				? inserts
					.map((item) => item.content.replace(/^\n+|\n+$/g, ''))
					.filter(Boolean)
					.join('\n')
				: undefined;

			return {
				referenceChangeId: sorted[0]?.changeId ?? '',
				kind: (inserts.length > 0 && deletes.length > 0
					? 'replace'
					: deletes.length > 0
						? 'delete'
						: 'insert') as AiChangeGroup['kind'],
				anchorLine: deletes.length > 0
					? Math.min(...deletes.map((d) => d.startLine))
					: Math.min(...inserts.map((i) => i.startLine)),
				deleteStartLine: deletes.length > 0 ? Math.min(...deletes.map((d) => d.startLine)) : undefined,
				deleteEndLine: deletes.length > 0 ? Math.max(...deletes.map((d) => d.endLine ?? d.startLine)) : undefined,
				insertContent,
			};
		})
		.sort((a, b) => {
			const aLine = a.deleteStartLine ?? a.anchorLine;
			const bLine = b.deleteStartLine ?? b.anchorLine;
			return aLine - bLine;
		});
}

function posAfterLine(doc: Text, lineNumber: number): number {
	if (lineNumber <= 0) return 0;
	const safeLine = Math.min(lineNumber, doc.lines);
	return doc.line(safeLine).to;
}

function createActionButton(
	label: string,
	title: string,
	className: string,
	handler: () => void,
): HTMLButtonElement {
	const btn = document.createElement('button');
	btn.type = 'button';
	btn.className = `cm-ai-btn ${className}`;
	btn.textContent = label;
	btn.title = title;
	btn.onclick = (e) => {
		e.preventDefault();
		e.stopPropagation();
		handler();
	};
	return btn;
}

class AiInsertedTextWidget extends WidgetType {
	constructor(
		private readonly content: string,
		private readonly changeId: string,
		private readonly kind: 'insert' | 'replace',
		private readonly onAccept: ChangeHandler,
		private readonly onReject: ChangeHandler,
	) {
		super();
	}

	eq(other: AiInsertedTextWidget): boolean {
		return this.content === other.content && this.changeId === other.changeId;
	}

	toDOM(): HTMLElement {
		const wrapper = document.createElement('div');
		wrapper.className = 'cm-ai-insert';

		const header = document.createElement('div');
		header.className = 'cm-ai-insert__header';

		const lineCount = this.content.split('\n').length;
		const kindLabel = this.kind === 'replace' ? 'Замена' : 'Вставка';
		const label = document.createElement('span');
		label.className = 'cm-ai-insert__label';
		label.textContent = `${kindLabel} (+${lineCount} стр.)`;

		const actions = document.createElement('div');
		actions.className = 'cm-ai-insert__actions';

		actions.append(
			createActionButton('Принять', 'Принять изменение', 'cm-ai-btn--accept', () => this.onAccept(this.changeId)),
			createActionButton('Отклонить', 'Отклонить изменение', 'cm-ai-btn--reject', () => this.onReject(this.changeId)),
		);

		header.append(label, actions);

		const body = document.createElement('div');
		body.className = 'cm-ai-insert__body';

		const pre = document.createElement('pre');
		pre.className = 'cm-ai-insert__content';
		pre.textContent = this.content;
		body.append(pre);

		const collapsed = this.content.split('\n').length > 12;
		if (collapsed) {
			body.classList.add('cm-ai-insert__body--collapsed');
		}

		const toggle = document.createElement('button');
		toggle.type = 'button';
		toggle.className = 'cm-ai-insert__toggle';
		toggle.textContent = collapsed ? 'Показать полностью' : 'Свернуть';
		if (!collapsed) toggle.style.display = 'none';

		toggle.onclick = (e) => {
			e.preventDefault();
			e.stopPropagation();
			const isCollapsed = body.classList.toggle('cm-ai-insert__body--collapsed');
			toggle.textContent = isCollapsed ? 'Показать полностью' : 'Свернуть';
			toggle.style.display = '';
		};

		wrapper.append(header, body, toggle);
		return wrapper;
	}

	ignoreEvent(): boolean {
		return false;
	}
}

class AiDeleteWidget extends WidgetType {
	constructor(
		private readonly lineCount: number,
		private readonly changeId: string,
		private readonly onAccept: ChangeHandler,
		private readonly onReject: ChangeHandler,
	) {
		super();
	}

	eq(other: AiDeleteWidget): boolean {
		return this.changeId === other.changeId && this.lineCount === other.lineCount;
	}

	toDOM(): HTMLElement {
		const wrapper = document.createElement('div');
		wrapper.className = 'cm-ai-delete-card';

		const header = document.createElement('div');
		header.className = 'cm-ai-delete-card__header';

		const label = document.createElement('span');
		label.className = 'cm-ai-delete-card__label';
		label.textContent = `Удаление (-${this.lineCount} стр.)`;

		const actions = document.createElement('div');
		actions.className = 'cm-ai-delete-card__actions';

		actions.append(
			createActionButton('Принять', 'Принять удаление', 'cm-ai-btn--accept', () => this.onAccept(this.changeId)),
			createActionButton('Отклонить', 'Отклонить удаление', 'cm-ai-btn--reject', () => this.onReject(this.changeId)),
		);

		header.append(label, actions);
		wrapper.append(header);
		return wrapper;
	}

	ignoreEvent(): boolean {
		return false;
	}
}

function buildDecorations(
	doc: Text,
	getChanges: () => DocumentAiChange[],
	onAccept: ChangeHandler,
	onReject: ChangeHandler,
): DecorationSet {
	const builder = new RangeSetBuilder<Decoration>();
	const groups = groupChanges(getChanges());

	let lastPos = 0;

	for (const group of groups) {
		if (group.deleteStartLine !== undefined && group.deleteEndLine !== undefined && doc.length > 0) {
			const from = doc.line(Math.min(group.deleteStartLine, doc.lines)).from;
			const to = doc.line(Math.min(group.deleteEndLine, doc.lines)).to;

			if (from >= lastPos) {
				builder.add(from, to, Decoration.mark({ class: 'cm-ai-delete-range' }));

				if (group.kind === 'delete') {
					const deleteLineCount = group.deleteEndLine - group.deleteStartLine + 1;
					builder.add(
						to,
						to,
						Decoration.widget({
							widget: new AiDeleteWidget(deleteLineCount, group.referenceChangeId, onAccept, onReject),
							side: 1,
							block: true,
						}),
					);
				}

				lastPos = to;
			}
		}

		if (group.insertContent) {
			const pos = posAfterLine(doc, group.deleteEndLine ?? group.anchorLine);

			if (pos >= lastPos) {
				builder.add(
					pos,
					pos,
					Decoration.widget({
						widget: new AiInsertedTextWidget(group.insertContent, group.referenceChangeId, group.kind as 'insert' | 'replace', onAccept, onReject),
						side: 1,
						block: true,
					}),
				);
				lastPos = pos;
			}
		}
	}

	return builder.finish();
}

export function createAiChangeExtensions(
	getChanges: () => DocumentAiChange[],
	onAccept: ChangeHandler,
	onReject: ChangeHandler,
): Extension {
	const decorationsField = StateField.define<DecorationSet>({
		create(state) {
			return buildDecorations(state.doc, getChanges, onAccept, onReject);
		},
		update(decorations, transaction) {
			const shouldRefresh =
				transaction.docChanged ||
				transaction.effects.some((effect) => effect.is(refreshAiChangeDecorationsEffect));

			if (!shouldRefresh) {
				return decorations.map(transaction.changes);
			}

			return buildDecorations(transaction.state.doc, getChanges, onAccept, onReject);
		},
		provide: (field) => EditorView.decorations.from(field),
	});

	return [decorationsField];
}

export const aiChangeStyles = EditorView.baseTheme({
	'.cm-ai-delete-range': {
		backgroundColor: 'rgba(239, 68, 68, 0.14)',
		color: '#b91c1c',
		textDecoration: 'line-through',
		textDecorationThickness: '2px',
		textDecorationColor: 'rgba(220, 38, 38, 0.75)',
		borderRadius: '3px',
	},
	'.cm-ai-delete-card': {
		display: 'block',
		margin: '6px 0',
		border: '1px solid rgba(239, 68, 68, 0.35)',
		borderRadius: '6px',
		overflow: 'hidden',
	},
	'.cm-ai-delete-card__header': {
		display: 'flex',
		alignItems: 'center',
		justifyContent: 'space-between',
		padding: '6px 10px',
		backgroundColor: 'rgba(239, 68, 68, 0.10)',
		borderBottom: '1px solid rgba(239, 68, 68, 0.25)',
	},
	'.cm-ai-delete-card__label': {
		fontSize: '12px',
		fontWeight: '600',
		color: '#b91c1c',
	},
	'.cm-ai-delete-card__actions': {
		display: 'flex',
		gap: '6px',
	},
	'.cm-ai-insert': {
		display: 'block',
		margin: '6px 0',
		border: '1px solid rgba(34, 197, 94, 0.35)',
		borderRadius: '6px',
		overflow: 'hidden',
	},
	'.cm-ai-insert__header': {
		display: 'flex',
		alignItems: 'center',
		justifyContent: 'space-between',
		padding: '6px 10px',
		backgroundColor: 'rgba(34, 197, 94, 0.12)',
		borderBottom: '1px solid rgba(34, 197, 94, 0.25)',
		position: 'sticky',
		top: '0',
		zIndex: '1',
	},
	'.cm-ai-insert__label': {
		fontSize: '12px',
		fontWeight: '600',
		color: '#15803d',
	},
	'.cm-ai-insert__actions': {
		display: 'flex',
		gap: '6px',
	},
	'.cm-ai-insert__body': {
		maxHeight: 'none',
		overflow: 'hidden',
	},
	'.cm-ai-insert__body--collapsed': {
		maxHeight: '180px',
	},
	'.cm-ai-insert__content': {
		margin: '0',
		padding: '8px 10px',
		whiteSpace: 'pre-wrap',
		backgroundColor: 'rgba(34, 197, 94, 0.07)',
		color: '#15803d',
		fontFamily: 'inherit',
		fontSize: 'inherit',
		lineHeight: 'inherit',
	},
	'.cm-ai-insert__toggle': {
		display: 'block',
		width: '100%',
		padding: '4px 10px',
		border: 'none',
		borderTop: '1px solid rgba(34, 197, 94, 0.2)',
		background: 'rgba(34, 197, 94, 0.06)',
		color: '#15803d',
		fontSize: '11px',
		cursor: 'pointer',
		textAlign: 'center',
	},
	'.cm-ai-btn': {
		padding: '3px 10px',
		borderRadius: '4px',
		border: '1px solid transparent',
		cursor: 'pointer',
		fontSize: '12px',
		fontWeight: '500',
		lineHeight: '1.4',
	},
	'.cm-ai-btn--accept': {
		color: '#15803d',
		backgroundColor: 'rgba(34, 197, 94, 0.15)',
		borderColor: 'rgba(34, 197, 94, 0.4)',
	},
	'.cm-ai-btn--reject': {
		color: '#b91c1c',
		backgroundColor: 'rgba(239, 68, 68, 0.1)',
		borderColor: 'rgba(239, 68, 68, 0.35)',
	},
});
