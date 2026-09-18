import { StrictMode } from "react";
import { createRoot } from "react-dom/client";

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
