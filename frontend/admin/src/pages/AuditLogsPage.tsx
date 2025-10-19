import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Table, Card, Select, DatePicker, Button, Space, Tag, Input } from 'antd';
import { ReloadOutlined, FilterOutlined, SearchOutlined, DownloadOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import type { Dayjs } from 'dayjs';
import { auditLogsApi, type AuditLog, type AuditLogsResponse } from '../lib/auditLogsApi';

const { RangePicker } = DatePicker;
const { Option } = Select;

export default function AuditLogsPage() {
  const { t } = useTranslation();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [category, setCategory] = useState<string>('');
  const [success, setSuccess] = useState<boolean | null>(null);
  const [dateRange, setDateRange] = useState<[Dayjs, Dayjs] | null>(null);
  const [keyword, setKeyword] = useState<string>('');

  const { data, isLoading, refetch } = useQuery<AuditLogsResponse>({
    queryKey: ['auditLogs', page, pageSize, category, success, dateRange, keyword],
    queryFn: () => auditLogsApi.getAll({
      page,
      pageSize,
      category: category || undefined,
      success: success !== null ? success : undefined,
      startDate: dateRange?.[0].toISOString(),
      endDate: dateRange?.[1].toISOString(),
      keyword: keyword || undefined,
    }),
  });

  const { data: categories } = useQuery<string[]>({
    queryKey: ['auditLogCategories'],
    queryFn: () => auditLogsApi.getCategories(),
  });

  const columns = [
    {
      title: t('auditLogs.timestamp'),
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 180,
      render: (text: string) => dayjs(text).format('YYYY-MM-DD HH:mm:ss'),
    },
    {
      title: t('auditLogs.user'),
      dataIndex: 'userName',
      key: 'userName',
      width: 150,
      render: (text: string | null) => text || '-',
    },
    {
      title: t('auditLogs.action'),
      dataIndex: 'action',
      key: 'action',
      width: 200,
    },
    {
      title: t('auditLogs.category'),
      dataIndex: 'category',
      key: 'category',
      width: 120,
      render: (text: string) => <Tag>{text}</Tag>,
    },
    {
      title: t('auditLogs.status'),
      dataIndex: 'success',
      key: 'success',
      width: 80,
      render: (success: boolean) => (
        <Tag color={success ? 'success' : 'error'}>
          {success ? t('auditLogs.succeeded') : t('auditLogs.failed')}
        </Tag>
      ),
    },
    {
      title: t('auditLogs.ipAddress'),
      dataIndex: 'ipAddress',
      key: 'ipAddress',
      width: 150,
      render: (text: string | null) => text || '-',
    },
    {
      title: t('auditLogs.details'),
      dataIndex: 'details',
      key: 'details',
      ellipsis: true,
      render: (text: string | null, record: AuditLog) => {
        if (record.errorMessage) {
          return <span className="text-red-600">{record.errorMessage}</span>;
        }
        return text || '-';
      },
    },
  ];

  const handleReset = () => {
    setCategory('');
    setSuccess(null);
    setDateRange(null);
    setPage(1);
    setKeyword('');
  };

  const handleExport = () => {
    const params = new URLSearchParams();
    params.append('page', page.toString());
    params.append('pageSize', pageSize.toString());
    if (category) params.append('category', category);
    if (success !== null) params.append('success', success.toString());
    if (dateRange) {
      params.append('startDate', dateRange[0].toISOString());
      params.append('endDate', dateRange[1].toISOString());
    }
    if (keyword.trim()) params.append('keyword', keyword.trim());

    const url = `/api/auditlogs/export?${params.toString()}`;
    window.open(url, '_blank');
  };

  return (
    <div className="p-6">
      <Card
        title={
          <div className="flex items-center justify-between">
            <span className="text-xl font-semibold">{t('auditLogs.title')}</span>
            <Button
              icon={<ReloadOutlined />}
              onClick={() => refetch()}
              loading={isLoading}
            >
              {t('auditLogs.refresh')}
            </Button>
          </div>
        }
      >
        <Space direction="vertical" size="middle" className="w-full">
          {/* Filter */}
          <Card size="small" title={<><FilterOutlined /> {t('auditLogs.filterConditions')}</>}>
            <Space wrap>
              <Select
                placeholder={t('auditLogs.selectCategory')}
                style={{ width: 150 }}
                value={category || undefined}
                onChange={setCategory}
                allowClear
              >
                {categories?.map((cat) => (
                  <Option key={cat} value={cat}>
                    {cat}
                  </Option>
                ))}
              </Select>

              <Select
                placeholder={t('auditLogs.selectStatus')}
                style={{ width: 120 }}
                value={success === null ? undefined : success}
                onChange={(value) => setSuccess(value)}
                allowClear
              >
                <Option value={true}>{t('auditLogs.succeeded')}</Option>
                <Option value={false}>{t('auditLogs.failed')}</Option>
              </Select>

              <RangePicker
                value={dateRange}
                onChange={(dates) => setDateRange(dates as [Dayjs, Dayjs] | null)}
                showTime
                format="YYYY-MM-DD HH:mm"
              />

              <Input
                placeholder={t('auditLogs.searchPlaceholder')}
                value={keyword}
                onChange={(e) => setKeyword(e.target.value)}
                allowClear
                prefix={<SearchOutlined />}
                style={{ width: 240 }}
              />

              <Button onClick={handleReset}>{t('auditLogs.reset')}</Button>
              <Button icon={<DownloadOutlined />} onClick={handleExport}>
                {t('auditLogs.exportCsv')}
              </Button>
            </Space>
          </Card>

          {/* Table */}
          <Table
            columns={columns}
            dataSource={data?.logs || []}
            rowKey="id"
            loading={isLoading}
            pagination={{
              current: page,
              pageSize: pageSize,
              total: data?.total || 0,
              showSizeChanger: true,
              showQuickJumper: true,
              showTotal: (total) => t('auditLogs.totalRecords', { total }),
              onChange: (newPage, newPageSize) => {
                setPage(newPage);
                setPageSize(newPageSize);
              },
            }}
            scroll={{ x: 1200 }}
          />
        </Space>
      </Card>
    </div>
  );
}
