import type { ReactNode } from "react";

import "./bo-cuc.css";

export interface MucDieuHuong {
  ma: string;
  nhan: string;
}

export interface BoCucProps {
  tieuDeTrang: string;
  cacMuc: readonly MucDieuHuong[];
  maMucDangChon: string;
  children: ReactNode;
}

export function BoCuc({ tieuDeTrang, cacMuc, maMucDangChon, children }: BoCucProps) {
  return (
    <div className="bo-cuc">
      <nav className="thanh-ben" aria-label="Điều hướng chính">
        <div className="thanh-ben-thuong-hieu">BA STaxi</div>
        <ul className="thanh-ben-danh-sach">
          {cacMuc.map((muc) => (
            <li key={muc.ma}>
              <a
                className={muc.ma === maMucDangChon ? "thanh-ben-muc thanh-ben-muc-dang-chon" : "thanh-ben-muc"}
                href={`#${muc.ma}`}
                aria-current={muc.ma === maMucDangChon ? "page" : undefined}
              >
                {muc.nhan}
              </a>
            </li>
          ))}
        </ul>
      </nav>
      <main className="vung-noi-dung">
        <header className="tieu-de-trang">
          <h1>{tieuDeTrang}</h1>
        </header>
        <div className="vung-card">{children}</div>
      </main>
    </div>
  );
}
