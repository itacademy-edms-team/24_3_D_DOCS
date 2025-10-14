import { Suspense, type PropsWithChildren } from "react";

type PreloadPageProps = PropsWithChildren;

export const PreloadPage = ({ children }: PreloadPageProps): React.JSX.Element => (
  <Suspense fallback={<>загрузка страницы</>}>
    {children}
  </Suspense>
)
