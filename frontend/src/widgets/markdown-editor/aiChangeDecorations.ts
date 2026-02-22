import {
	RangeSetBuilder,
	type Extension,
} from '@codemirror/state';
import {
	Decoration,
	type DecorationSet,
	EditorView,
	ViewPlugin,
	type ViewUpdate,
	WidgetType,
} from '@codemirror/view';

type ChangeHandler = (changeId: string, changeType: 'insert' | 'delete') => void;

interface ParsedMarker {
	changeId: string;
	changeType: 'insert' | 'delete';
	startMarkerFrom: number;
	startMarkerTo: number;
	contentFrom: number;
	contentTo: number;
	endMarkerFrom: number;
	endMarkerTo: number;
}

const AI_MARKER_REGEX = /<!-- AI:(INSERT|DELETE):(\w+) -->([\s\S]*?)<!-- \/AI:\1:\2 -->/g;

function parseMarkersFromDoc(docText: string): ParsedMarker[] {
	const markers: ParsedMarker[] = [];
	let match;
	
	while ((match = AI_MARKER_REGEX.exec(docText)) !== null) {
		const changeType = match[1].toLowerCase() as 'insert' | 'delete';
		const changeId = match[2];
		const fullMatchStart = match.index;
		const fullMatchEnd = fullMatchStart + match[0].length;
		
		const startMarkerText = `<!-- AI:${match[1]}:${changeId} -->`;
		const endMarkerText = `<!-- /AI:${match[1]}:${changeId} -->`;
		
		markers.push({
			changeId,
			changeType,
			startMarkerFrom: fullMatchStart,
			startMarkerTo: fullMatchStart + startMarkerText.length,
			contentFrom: fullMatchStart + startMarkerText.length,
			contentTo: fullMatchEnd - endMarkerText.length,
			endMarkerFrom: fullMatchEnd - endMarkerText.length,
			endMarkerTo: fullMatchEnd,
		});
	}
	
	AI_MARKER_REGEX.lastIndex = 0;
	return markers;
}

class AiActionButtonsWidget extends WidgetType {
	constructor(
		private readonly changeId: string,
		private readonly changeType: 'insert' | 'delete',
		private readonly onAccept: ChangeHandler,
		private readonly onUndo: ChangeHandler
	) {
		super();
	}

	eq(other: AiActionButtonsWidget): boolean {
		return this.changeId === other.changeId && this.changeType === other.changeType;
	}

	toDOM(): HTMLElement {
		const wrapper = document.createElement('span');
		wrapper.className = 'cm-ai-actions';

		const acceptButton = document.createElement('button');
		acceptButton.type = 'button';
		acceptButton.className = 'cm-ai-btn cm-ai-btn--accept';
		acceptButton.textContent = '✓';
		acceptButton.title = 'Принять';
		acceptButton.addEventListener('click', (e) => {
			e.preventDefault();
			e.stopPropagation();
			this.onAccept(this.changeId, this.changeType);
		});

		const undoButton = document.createElement('button');
		undoButton.type = 'button';
		undoButton.className = 'cm-ai-btn cm-ai-btn--undo';
		undoButton.textContent = '✕';
		undoButton.title = 'Отклонить';
		undoButton.addEventListener('click', (e) => {
			e.preventDefault();
			e.stopPropagation();
			this.onUndo(this.changeId, this.changeType);
		});

		wrapper.append(acceptButton, undoButton);
		return wrapper;
	}

	ignoreEvent(): boolean {
		return false;
	}
}

function buildDecorations(
	view: EditorView,
	onAccept: ChangeHandler,
	onUndo: ChangeHandler
): DecorationSet {
	const docText = view.state.doc.toString();
	const markers = parseMarkersFromDoc(docText);
	
	if (markers.length === 0) {
		return Decoration.none;
	}

	const builder = new RangeSetBuilder<Decoration>();
	
	const sortedDecos: Array<{ from: number; to: number; deco: Decoration }> = [];

	for (const marker of markers) {
		const markerClass = marker.changeType === 'insert' 
			? 'cm-ai-marker cm-ai-marker--insert' 
			: 'cm-ai-marker cm-ai-marker--delete';
		
		sortedDecos.push({
			from: marker.startMarkerFrom,
			to: marker.startMarkerTo,
			deco: Decoration.mark({ class: markerClass }),
		});
		
		sortedDecos.push({
			from: marker.endMarkerFrom,
			to: marker.endMarkerTo,
			deco: Decoration.mark({ class: markerClass }),
		});
		
		const contentClass = marker.changeType === 'insert'
			? 'cm-ai-content cm-ai-content--insert'
			: 'cm-ai-content cm-ai-content--delete';
		
		const contentStart = marker.contentFrom;
		const contentEnd = marker.contentTo;
		
		if (contentEnd > contentStart) {
			let actualContentStart = contentStart;
			if (docText[contentStart] === '\n') {
				actualContentStart = contentStart + 1;
			}
			let actualContentEnd = contentEnd;
			if (docText[contentEnd - 1] === '\n') {
				actualContentEnd = contentEnd - 1;
			}
			
			if (actualContentEnd > actualContentStart) {
				sortedDecos.push({
					from: actualContentStart,
					to: actualContentEnd,
					deco: Decoration.mark({ class: contentClass }),
				});
			}
		}
		
		sortedDecos.push({
			from: marker.endMarkerTo,
			to: marker.endMarkerTo,
			deco: Decoration.widget({
				widget: new AiActionButtonsWidget(marker.changeId, marker.changeType, onAccept, onUndo),
				side: 1,
			}),
		});
	}
	
	sortedDecos.sort((a, b) => a.from - b.from || a.to - b.to);
	
	for (const { from, to, deco } of sortedDecos) {
		builder.add(from, to, deco);
	}

	return builder.finish();
}

export function createAiChangeExtensions(
	onAccept: ChangeHandler,
	onUndo: ChangeHandler
): Extension {
	return ViewPlugin.fromClass(
		class {
			decorations: DecorationSet;

			constructor(view: EditorView) {
				this.decorations = buildDecorations(view, onAccept, onUndo);
			}

			update(update: ViewUpdate) {
				if (update.docChanged || update.viewportChanged) {
					this.decorations = buildDecorations(update.view, onAccept, onUndo);
				}
			}
		},
		{
			decorations: (v) => v.decorations,
		}
	);
}

export const aiChangeStyles = EditorView.baseTheme({
	'.cm-ai-marker': {
		fontSize: '0.75em',
		color: '#9ca3af',
		fontFamily: 'monospace',
		opacity: '0.7',
	},
	'.cm-ai-marker--insert': {
		color: '#16a34a',
	},
	'.cm-ai-marker--delete': {
		color: '#dc2626',
	},
	'.cm-ai-content--insert': {
		backgroundColor: 'rgba(34, 197, 94, 0.15)',
		borderRadius: '2px',
	},
	'.cm-ai-content--delete': {
		backgroundColor: 'rgba(239, 68, 68, 0.15)',
		textDecoration: 'line-through',
		textDecorationColor: 'rgba(239, 68, 68, 0.5)',
	},
	'.cm-ai-actions': {
		display: 'inline-flex',
		gap: '4px',
		marginLeft: '8px',
		verticalAlign: 'middle',
	},
	'.cm-ai-btn': {
		display: 'inline-flex',
		alignItems: 'center',
		justifyContent: 'center',
		width: '20px',
		height: '20px',
		border: '1px solid',
		borderRadius: '4px',
		cursor: 'pointer',
		fontSize: '12px',
		fontWeight: 'bold',
		lineHeight: '1',
		padding: '0',
	},
	'.cm-ai-btn--accept': {
		backgroundColor: 'rgba(34, 197, 94, 0.2)',
		borderColor: 'rgba(34, 197, 94, 0.4)',
		color: '#16a34a',
	},
	'.cm-ai-btn--accept:hover': {
		backgroundColor: 'rgba(34, 197, 94, 0.3)',
	},
	'.cm-ai-btn--undo': {
		backgroundColor: 'rgba(239, 68, 68, 0.2)',
		borderColor: 'rgba(239, 68, 68, 0.4)',
		color: '#dc2626',
	},
	'.cm-ai-btn--undo:hover': {
		backgroundColor: 'rgba(239, 68, 68, 0.3)',
	},
});
