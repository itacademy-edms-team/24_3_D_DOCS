import ReactDOM from "react-dom/client";
import { App } from "./app";
import './app/styles/global.css';

const rootEl = document.getElementById("root") as HTMLDivElement;
if (!rootEl) throw new Error("not found root element in html");

const root = ReactDOM.createRoot(rootEl);
root.render(<App />);
