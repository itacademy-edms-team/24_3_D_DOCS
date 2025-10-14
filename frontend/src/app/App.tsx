import React from "react";
import { RouterProvider } from "react-router-dom";
import { HelmetProvider } from "react-helmet-async";

import { ThemeProvider } from '@gravity-ui/uikit';

import router from "./routes";

export const App = (): React.JSX.Element => {
	return (
		<React.StrictMode>
			<ThemeProvider>
				<HelmetProvider>
					<RouterProvider router={router} />
				</HelmetProvider>
			</ThemeProvider>
		</React.StrictMode>
	);
};
