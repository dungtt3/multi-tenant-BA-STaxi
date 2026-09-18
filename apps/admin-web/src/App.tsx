import Badge from "react-bootstrap/Badge";
import Button from "react-bootstrap/Button";
import Table from "react-bootstrap/Table";

import { BoCuc, type MucDieuHuong } from "./components/BoCuc";
import { Ibox } from "./components/Ibox";

const CAC_MUC: readonly MucDieuHuong[] = [
  { ma: "tong-quan", nhan: "Tổng quan" },
  { ma: "danh-muc", nhan: "Danh mục" },
  { ma: "cau-hinh", nhan: "Cấu hình" },
  { ma: "bao-cao", nhan: "Báo cáo" },
];

interface DongLoaiXe {
  ma: string;
  ten: string;
  soCho: number;
  trangThai: string;
}

const CAC_DONG: readonly DongLoaiXe[] = [
  { ma: "LX01", ten: "Xe 4 chỗ", soCho: 4, trangThai: "Đang dùng" },
  { ma: "LX02", ten: "Xe 7 chỗ", soCho: 7, trangThai: "Đang dùng" },
  { ma: "LX03", ten: "Xe 16 chỗ", soCho: 16, trangThai: "Ngừng" },
];

export function App() {
  return (
    <BoCuc tieuDeTrang="Bảng token giao diện" cacMuc={CAC_MUC} maMucDangChon="tong-quan">
      <Ibox
        tieuDe="Nút và liên kết"
        hanhDong={<Button size="sm" variant="primary">Thêm mới</Button>}
      >
        <div className="d-flex flex-wrap align-items-center gap-2">
          <Button variant="primary">Nút chính</Button>
          <Button variant="outline-primary">Nút phụ</Button>
          <Button variant="link" href="#lien-ket">
            Liên kết trên nền xám
          </Button>
        </div>
        <p className="mt-3 mb-0 small">
          Nền nút dùng <code>#337ab7</code>. Chữ và liên kết trên nền xám dùng <code>#286090</code> vì
          <code> #337ab7</code> chỉ đạt 4,11:1 trên nền <code>#f3f3f4</code> — trượt chuẩn WCAG AA.
        </p>
      </Ibox>

      <Ibox tieuDe="Bảng ở cỡ chữ phụ">
        <Table className="mb-0" hover responsive>
          <thead>
            <tr>
              <th scope="col">Mã</th>
              <th scope="col">Tên loại xe</th>
              <th scope="col">Số chỗ</th>
              <th scope="col">Trạng thái</th>
            </tr>
          </thead>
          <tbody>
            {CAC_DONG.map((dong) => (
              <tr key={dong.ma}>
                <td>{dong.ma}</td>
                <td>{dong.ten}</td>
                <td>{dong.soCho}</td>
                <td>{dong.trangThai}</td>
              </tr>
            ))}
          </tbody>
        </Table>
      </Ibox>

      <Ibox tieuDe="Màu trạng thái">
        <div className="d-flex flex-wrap gap-2">
          <Badge bg="success">Thành công</Badge>
          <Badge bg="info">Thông tin</Badge>
          <Badge bg="warning" text="dark">
            Cảnh báo
          </Badge>
          <Badge bg="danger">Nguy hiểm</Badge>
        </div>
        <p className="mt-3 mb-0 small">
          Badge cảnh báo dùng chữ sẫm: chữ trắng trên nền <code>#f8ac59</code> chỉ đạt khoảng 2:1.
        </p>
      </Ibox>
    </BoCuc>
  );
}
