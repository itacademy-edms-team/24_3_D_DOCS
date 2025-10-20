import { useEffect, useRef, useState } from 'react';
import styles from './CodeInput.module.css';

interface CodeInputProps {
	length?: number;
	value: string;
	onChange: (value: string) => void;
	onComplete?: (value: string) => void;
	autoFocus?: boolean;
}

export const CodeInput = ({ 
	length = 6, 
	value, 
	onChange, 
	onComplete,
	autoFocus = false 
}: CodeInputProps) => {
	const [focused, setFocused] = useState(-1);
	const inputRefs = useRef<(HTMLInputElement | null)[]>([]);

	const digits = value.split('').slice(0, length);
	while (digits.length < length) {
		digits.push('');
	}

	useEffect(() => {
		if (autoFocus && inputRefs.current[0]) {
			inputRefs.current[0].focus();
		}
	}, [autoFocus]);

	const handleChange = (index: number, inputValue: string) => {
		const digit = inputValue.replace(/\D/g, '').slice(-1);
		
		const newDigits = [...digits];
		newDigits[index] = digit;
		const newValue = newDigits.join('');
		
		onChange(newValue);

		if (digit && index < length - 1) {
			inputRefs.current[index + 1]?.focus();
		}

		if (newValue.length === length && onComplete) {
			onComplete(newValue);
		}
	};

	const handleKeyDown = (index: number, e: React.KeyboardEvent<HTMLInputElement>) => {
		if (e.key === 'Backspace') {
			if (!digits[index] && index > 0) {
				inputRefs.current[index - 1]?.focus();
			}
		} else if (e.key === 'ArrowLeft' && index > 0) {
			e.preventDefault();
			inputRefs.current[index - 1]?.focus();
		} else if (e.key === 'ArrowRight' && index < length - 1) {
			e.preventDefault();
			inputRefs.current[index + 1]?.focus();
		}
	};

	const handlePaste = (e: React.ClipboardEvent) => {
		e.preventDefault();
		const pastedData = e.clipboardData.getData('text').replace(/\D/g, '').slice(0, length);
		onChange(pastedData);
		
		if (pastedData.length === length) {
			inputRefs.current[length - 1]?.focus();
			if (onComplete) {
				onComplete(pastedData);
			}
		} else if (pastedData.length > 0) {
			inputRefs.current[Math.min(pastedData.length, length - 1)]?.focus();
		}
	};

	return (
		<div className={styles.container}>
			{digits.map((digit, index) => (
				<input
					key={index}
					ref={(el) => (inputRefs.current[index] = el)}
					type="text"
					inputMode="numeric"
					maxLength={1}
					value={digit}
					onChange={(e) => handleChange(index, e.target.value)}
					onKeyDown={(e) => handleKeyDown(index, e)}
					onFocus={() => setFocused(index)}
					onBlur={() => setFocused(-1)}
					onPaste={handlePaste}
					className={`${styles.input} ${focused === index ? styles.focused : ''} ${digit ? styles.filled : ''}`}
					aria-label={`Digit ${index + 1}`}
				/>
			))}
		</div>
	);
};

