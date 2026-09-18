import type { ReactNode } from "react";

import "./ibox.css";

export interface IboxProps {
  tieuDe: string;
  children: ReactNode;
  hanhDong?: ReactNode;
}

export function Ibox({ tieuDe, children, hanhDong }: IboxProps) {
  return (
    <section className="ibox">
      <header className="ibox-title">
        <h3 className="ibox-title-chu">{tieuDe}</h3>
        {hanhDong ? <div className="ibox-title-hanh-dong">{hanhDong}</div> : null}
      </header>
      <div className="ibox-content">{children}</div>
    </section>
  );
}
