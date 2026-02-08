import { lineNumbers } from '@codemirror/view';
import { config } from 'md-editor-v3';
import type { KatexOptions } from 'katex';

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
	katexConfig(options: KatexOptions) {
		return {
			...options,
			strict: 'ignore',
		};
	},
});
