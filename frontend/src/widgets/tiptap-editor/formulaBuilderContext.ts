import type { InjectionKey } from 'vue';

export type OpenFormulaBuilderFn = (opts: {
	initialLatex: string;
	isBlock: boolean;
	onApply: (latex: string) => void;
}) => void;

export const formulaBuilderKey: InjectionKey<OpenFormulaBuilderFn> = Symbol('openFormulaBuilder');
