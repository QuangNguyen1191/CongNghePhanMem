import { Form, Input, Modal } from 'antd';
import { useState } from 'react';
import { post } from '../api/client';
import { useThaoTac } from './Chung';

export function DoiMatKhau({ mo, dong }: { mo: boolean; dong: () => void }) {
  const [form] = Form.useForm();
  const [dangGui, setDangGui] = useState(false);
  const thaoTac = useThaoTac();

  const gui = async () => {
    const v = await form.validateFields();
    setDangGui(true);
    const kq = await thaoTac(() => post('/api/tai-khoan/doi-mat-khau', { matKhauCu: v.cu, matKhauMoi: v.moi }), 'Đã đổi mật khẩu');
    setDangGui(false);
    if (kq !== undefined) {
      form.resetFields();
      dong();
    }
  };

  return (
    <Modal open={mo} onCancel={dong} onOk={gui} okText="Đổi mật khẩu" cancelText="Hủy" confirmLoading={dangGui} title="Đổi mật khẩu" destroyOnClose>
      <Form form={form} layout="vertical" requiredMark={false} onFinish={gui}>
        <Form.Item name="cu" label="Mật khẩu hiện tại" rules={[{ required: true, message: 'Nhập mật khẩu hiện tại' }]}>
          <Input.Password autoComplete="current-password" autoFocus />
        </Form.Item>
        <Form.Item name="moi" label="Mật khẩu mới" rules={[{ required: true, min: 6, message: 'Ít nhất 6 ký tự' }]}>
          <Input.Password autoComplete="new-password" />
        </Form.Item>
        <Form.Item
          name="lai"
          label="Nhập lại mật khẩu mới"
          dependencies={['moi']}
          rules={[
            { required: true, message: 'Nhập lại mật khẩu mới' },
            ({ getFieldValue }) => ({
              validator: (_, v) => (!v || v === getFieldValue('moi') ? Promise.resolve() : Promise.reject(new Error('Hai mật khẩu chưa khớp'))),
            }),
          ]}
        >
          <Input.Password autoComplete="new-password" />
        </Form.Item>
        <button type="submit" hidden />
      </Form>
    </Modal>
  );
}
