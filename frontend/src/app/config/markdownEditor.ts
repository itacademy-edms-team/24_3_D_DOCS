import { lineNumbers } from '@codemirror/view';
import { config } from 'md-editor-v3';

config({
	codeMirrorExtensions(extensions) {
		return [
			...extensions,
			{
				type: 'lineNumbers',
				extension: lineNumbers(),
			},
		];
	},
});
