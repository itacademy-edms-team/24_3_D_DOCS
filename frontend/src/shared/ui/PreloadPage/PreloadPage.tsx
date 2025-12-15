import { Suspense, type PropsWithChildren } from "react";
import { Loader } from '../Loader';

type PreloadPageProps = PropsWithChildren;

export const PreloadPage = ({ children }: PreloadPageProps): React.JSX.Element => (
  <Suspense fallback={
    <div style={{ 
      display: 'flex', 
      justifyContent: 'center', 
      alignItems: 'center', 
      height: '100vh',
      background: '#09090b'
    }}>
      <Loader />
    </div>
  }>
    {children}
  </Suspense>
);
