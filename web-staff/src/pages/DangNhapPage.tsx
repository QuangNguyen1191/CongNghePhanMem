import { LockOutlined, UserOutlined } from '@ant-design/icons';
import { Alert, Button, Form, Input } from 'antd';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { loiThanhChu } from '../components/Chung';
import { usePhien } from '../lib/phien';

export default function DangNhapPage() {
  const { dangNhap } = usePhien();
  const nav = useNavigate();
  const [loi, setLoi] = useState<string | null>(null);
  const [dangGui, setDangGui] = useState(false);

  const gui = async (v: { tenDangNhap: string; matKhau: string }) => {
    setLoi(null);
    setDangGui(true);
    try {
      await dangNhap(v.tenDangNhap.trim(), v.matKhau);
      nav('/', { replace: true });
    } catch (e) {
      setLoi(loiThanhChu(e));
    } finally {
      setDangGui(false);
    }
  };

  return (
    <main className="dang-nhap">
      <section className="dang-nhap-trai" aria-hidden>
        <div className="logo-o" style={{ width: 56, height: 56, fontSize: 15, lineHeight: '15px' }}>
          UNI
          <br />
          QLO
        </div>
        <div>
          <h1>
            Hệ thống
            <br />
            nhân viên cửa hàng
          </h1>
          <p>Bán tại quầy, xử lý đơn online, đổi trả, hoàn tiền và nhập hàng trên cùng một màn hình. Mọi thao tác chính đều có phím tắt.</p>
        </div>
        <small style={{ color: '#737373' }}>Web staff · Đồ án Công nghệ phần mềm</small>
      </section>
      <section className="dang-nhap-phai">
        <h2>Đăng nhập</h2>
        <p style={{ color: '#525252', marginTop: 0, marginBottom: 24 }}>Dành cho nhân viên và quản lý cửa hàng.</p>
        {loi && <Alert type="error" showIcon message={loi} style={{ marginBottom: 16 }} role="alert" />}
        <Form layout="vertical" requiredMark={false} onFinish={gui} size="large">
          <Form.Item name="tenDangNhap" label="Tên đăng nhập" rules={[{ required: true, message: 'Nhập tên đăng nhập' }]}>
            <Input prefix={<UserOutlined aria-hidden />} autoComplete="username" autoFocus placeholder="vd: thungan1" />
          </Form.Item>
          <Form.Item name="matKhau" label="Mật khẩu" rules={[{ required: true, message: 'Nhập mật khẩu' }]}>
            <Input.Password prefix={<LockOutlined aria-hidden />} autoComplete="current-password" />
          </Form.Item>
          <Button type="primary" htmlType="submit" block loading={dangGui}>
            Đăng nhập
          </Button>
        </Form>
        <p style={{ color: '#737373', fontSize: 12, marginTop: 24 }}>
          Tài khoản khách hàng không đăng nhập được ở đây. Dữ liệu mẫu: <code>thungan1</code>, <code>thukho</code>, <code>banhangonline</code>,{' '}
          <code>quanly</code> (mật khẩu <code>123456</code>).
        </p>
      </section>
    </main>
  );
}
