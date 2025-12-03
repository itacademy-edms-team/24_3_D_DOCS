import { mmToPx, PAGE_WIDTH_MM, PAGE_HEIGHT_MM } from '../../application/services/canvasUtils';

interface TitlePageCanvasRulerProps {
  orientation: 'horizontal' | 'vertical';
  mousePos?: { x: number; y: number } | null;
}

export function TitlePageCanvasRuler({ orientation, mousePos }: TitlePageCanvasRulerProps) {
  const length = orientation === 'horizontal' ? PAGE_WIDTH_MM : PAGE_HEIGHT_MM;
  const step = length > 200 ? 10 : length > 100 ? 5 : 1; // Adaptive step size
  
  const marks: JSX.Element[] = [];
  
  for (let i = 0; i <= length; i += step) {
    const isMajor = i % (step * 5) === 0;
    const size = isMajor ? 15 : 8;
    const label = isMajor ? i.toString() : '';
    
    if (orientation === 'horizontal') {
      marks.push(
        <div
          key={i}
          style={{
            position: 'absolute',
            left: `${mmToPx(i)}px`,
            top: 0,
            width: '1px',
            height: `${size}px`,
            background: '#666',
          }}
        />
      );
      if (label) {
        marks.push(
          <div
            key={`label-${i}`}
            style={{
              position: 'absolute',
              left: `${mmToPx(i) + 2}px`,
              top: 2,
              fontSize: '10px',
              color: '#666',
              pointerEvents: 'none',
            }}
          >
            {label}
          </div>
        );
      }
    } else {
      marks.push(
        <div
          key={i}
          style={{
            position: 'absolute',
            top: `${mmToPx(i)}px`,
            left: 0,
            height: '1px',
            width: `${size}px`,
            background: '#666',
          }}
        />
      );
      if (label) {
        marks.push(
          <div
            key={`label-${i}`}
            style={{
              position: 'absolute',
              top: `${mmToPx(i) + 2}px`,
              left: 2,
              fontSize: '10px',
              color: '#666',
              pointerEvents: 'none',
              transform: 'rotate(-90deg)',
              transformOrigin: 'top left',
            }}
          >
            {label}
          </div>
        );
      }
    }
  }

  if (orientation === 'horizontal') {
    return (
      <div
        style={{
          position: 'absolute',
          top: '-20px',
          left: 0,
          width: `${mmToPx(PAGE_WIDTH_MM)}px`,
          height: '20px',
          background: '#e8e8e8',
          borderBottom: '1px solid #ccc',
        }}
      >
        {marks}
        {mousePos && (
          <div
            style={{
              position: 'absolute',
              left: `${mmToPx(mousePos.x)}px`,
              top: 0,
              width: '1px',
              height: '20px',
              background: '#0066ff',
              pointerEvents: 'none',
            }}
          />
        )}
      </div>
    );
  } else {
    return (
      <div
        style={{
          position: 'absolute',
          top: 0,
          left: '-20px',
          width: '20px',
          height: `${mmToPx(PAGE_HEIGHT_MM)}px`,
          background: '#e8e8e8',
          borderRight: '1px solid #ccc',
        }}
      >
        {marks}
        {mousePos && (
          <div
            style={{
              position: 'absolute',
              top: `${mmToPx(mousePos.y)}px`,
              left: 0,
              width: '20px',
              height: '1px',
              background: '#0066ff',
              pointerEvents: 'none',
            }}
          />
        )}
      </div>
    );
  }
}

