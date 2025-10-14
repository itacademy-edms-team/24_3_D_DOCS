import { Suspense } from "react";
import { Outlet } from "react-router-dom";

import { Meta } from '../../../shared/ui';

export const AppLayout = (): React.JSX.Element => {
  return (
    <>
      <Meta
        lang="ru"
        title="Главная страница"
        description="На этой страницы главная часть приложения"
      />
      <div id="page-content">
        <Suspense>
          <Outlet />
        </Suspense>
      </div>
    </>
  );
}
