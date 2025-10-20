import React from "react";
import { RouterProvider } from "react-router-dom";
import { HelmetProvider } from "react-helmet-async";

import { AuthProvider } from './providers';
import router from "./routes";

export const App = (): React.JSX.Element => {
	return (
		<React.StrictMode>
			<HelmetProvider>
				<AuthProvider>
					<RouterProvider router={router} />
				</AuthProvider>
			</HelmetProvider>
		</React.StrictMode>
	);
};
