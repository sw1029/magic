import "./style.css";
import { mountApp } from "./app";

const root = document.querySelector<HTMLDivElement>("#app");

if (!root) {
  throw new Error("앱 루트를 찾지 못했습니다.");
}

mountApp(root);
