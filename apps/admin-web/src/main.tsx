import { StrictMode } from "react";
import { createRoot } from "react-dom/client";

import "@fontsource/open-sans/latin-400.css";
import "@fontsource/open-sans/latin-600.css";
import "@fontsource/open-sans/latin-700.css";
import "@fontsource/open-sans/latin-ext-400.css";
import "@fontsource/open-sans/latin-ext-600.css";
import "@fontsource/open-sans/vietnamese-400.css";
import "@fontsource/open-sans/vietnamese-600.css";
import "@fontsource/open-sans/vietnamese-700.css";
import "bootstrap/dist/css/bootstrap.min.css";
import "./styles/token.css";

import { App } from "./App";

const goc = document.getElementById("root");

if (!goc) {
  throw new Error("Không tìm thấy phần tử #root trong index.html");
}

createRoot(goc).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
