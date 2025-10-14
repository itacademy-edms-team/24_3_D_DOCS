import { lazy } from "react";
import { createBrowserRouter } from "react-router-dom";

import { ErrorPage, PreloadPage } from '@ui';

import { AppLayout } from './layouts';

const MainPage = lazy(() => import("../pages/main/ui/MainPage"));

const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: (
        <PreloadPage>
          <MainPage />
        </PreloadPage>
      )},
      { path: '*', element: <ErrorPage /> },
    ]
  }
])

export default router;
