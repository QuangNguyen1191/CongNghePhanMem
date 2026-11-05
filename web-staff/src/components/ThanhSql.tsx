import { DatabaseOutlined } from '@ant-design/icons';
import { Tooltip } from 'antd';
import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { ngheSql, type ThongTinSql } from '../api/client';

// Yêu cầu 1: hiện số câu SQL request vừa chạy và vị trí (file:dòng trong DAL) của câu cuối.
export function ThanhSql() {
  const [t, setT] = useState<ThongTinSql | null>(null);
  useEffect(() => ngheSql(setT), []);
  if (!t) return null;
  return (
    <Tooltip
      title={
        <div style={{ fontSize: 12 }}>
          <div>{t.duongDan}</div>
          <div>Câu cuối: {t.viTri ?? '(không chạy SQL)'}</div>
          <div>{t.thoiGianMs} ms</div>
        </div>
      }
    >
      <Link to="/nhat-ky-sql" className="thanh-sql" aria-label="Mở nhật ký SQL">
        <DatabaseOutlined aria-hidden />
        <span>
          <b>{t.soCau}</b> câu SQL
        </span>
        <span className="thanh-sql-vt">{t.viTri?.split(' ')[0] ?? '—'}</span>
      </Link>
    </Tooltip>
  );
}
