import { lazy } from "react";
import { createBrowserRouter, Navigate } from "react-router-dom";

import { ErrorPage, PreloadPage } from '@ui';
import { AppLayout } from './layouts';
import { ProtectedRoute } from './providers';

const AuthPage = lazy(() => import("../pages/auth"));
const MainPage = lazy(() => import("../pages/main"));

const router = createBrowserRouter([
  {
    path: '/auth',
    element: (
      <PreloadPage>
        <AuthPage />
      </PreloadPage>
    ),
  },
  {
    path: '/',
    element: <AppLayout />,
    children: [
      {
        index: true,
        element: (
          <ProtectedRoute>
            <PreloadPage>
              <MainPage />
            </PreloadPage>
          </ProtectedRoute>
        ),
      },
      {
        path: 'dashboard',
        element: (
          <ProtectedRoute>
            <PreloadPage>
              <MainPage />
            </PreloadPage>
          </ProtectedRoute>
        ),
      },
      { path: '*', element: <ErrorPage /> },
    ]
  }
])

export default router;
