import React, { useState, useEffect } from "react";
import {
  Card,
  Row,
  Col,
  Statistic,
  Table,
  Spin,
  Alert,
  Typography,
  Divider,
  Tag,
  Progress,
  DatePicker,
  Space,
} from "antd";
import {
  UserOutlined,
  LoginOutlined,
  ApiOutlined,
  WarningOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  TeamOutlined,
} from "@ant-design/icons";
import { useTranslation } from "react-i18next";
import dayjs from "dayjs";
import {
  analyticsApi,
  DashboardStatistics,
  LoginTrend,
  ApiCallStatistic,
} from "../lib/analyticsApi";

const { Title, Text } = Typography;
const { RangePicker } = DatePicker;

export default function DashboardPage() {
  const { t } = useTranslation();
  const [loading, setLoading] = useState(true);
  const [statistics, setStatistics] = useState<DashboardStatistics | null>(null);
  const [loginTrends, setLoginTrends] = useState<LoginTrend[]>([]);
  const [apiStats, setApiStats] = useState<ApiCallStatistic[]>([]);
  const [dateRange, setDateRange] = useState<[dayjs.Dayjs, dayjs.Dayjs]>([
    dayjs().subtract(30, "days"),
    dayjs(),
  ]);
  const [trendDays, setTrendDays] = useState(7);

  useEffect(() => {
    loadData();
  }, [dateRange]);

  const loadData = async () => {
    setLoading(true);
    try {
      const [stats, trends, apiCalls] = await Promise.all([
        analyticsApi.getDashboardStatistics(
          dateRange[0].toDate(),
          dateRange[1].toDate()
        ),
        analyticsApi.getLoginTrends(trendDays),
        analyticsApi.getApiCallStatistics(10),
      ]);

      setStatistics(stats);
      setLoginTrends(trends);
      setApiStats(apiCalls);
    } catch (error) {
      console.error("Failed to load dashboard data", error);
    } finally {
      setLoading(false);
    }
  };

  const loginTrendColumns = [
    {
      title: t('dashboard.date'),
      dataIndex: "date",
      key: "date",
      render: (date: string) => dayjs(date).format("YYYY-MM-DD"),
    },
    {
      title: t('dashboard.success'),
      dataIndex: "successfulLogins",
      key: "successfulLogins",
      render: (value: number) => (
        <Tag color="green">
          <CheckCircleOutlined /> {value}
        </Tag>
      ),
    },
    {
      title: t('dashboard.failed'),
      dataIndex: "failedLogins",
      key: "failedLogins",
      render: (value: number) => (
        <Tag color="red">
          <CloseCircleOutlined /> {value}
        </Tag>
      ),
    },
    {
      title: t('dashboard.total'),
      dataIndex: "totalLogins",
      key: "totalLogins",
    },
    {
      title: t('dashboard.successRate'),
      key: "successRate",
      render: (_: any, record: LoginTrend) => {
        const rate =
          record.totalLogins > 0
            ? (record.successfulLogins / record.totalLogins) * 100
            : 0;
        return <Progress percent={Math.round(rate)} size="small" />;
      },
    },
  ];

  const apiStatsColumns = [
    {
      title: t('dashboard.action'),
      dataIndex: "action",
      key: "action",
      ellipsis: true,
    },
    {
      title: t('dashboard.totalCallsCount'),
      dataIndex: "callCount",
      key: "callCount",
      sorter: (a: ApiCallStatistic, b: ApiCallStatistic) => a.callCount - b.callCount,
    },
    {
      title: t('dashboard.success'),
      dataIndex: "successCount",
      key: "successCount",
      render: (value: number) => <Text style={{ color: "#52c41a" }}>{value}</Text>,
    },
    {
      title: t('dashboard.failure'),
      dataIndex: "failureCount",
      key: "failureCount",
      render: (value: number) => <Text style={{ color: "#ff4d4f" }}>{value}</Text>,
    },
    {
      title: t('dashboard.successRate'),
      dataIndex: "successRate",
      key: "successRate",
      render: (rate: number) => <Progress percent={Math.round(rate)} size="small" />,
    },
  ];

  if (loading) {
    return (
      <div style={{ textAlign: "center", padding: "50px" }}>
        <Spin size="large" />
      </div>
    );
  }

  if (!statistics) {
    return <Alert message={t('dashboard.loadFailed')} type="error" />;
  }

  return (
    <div>
      <Row justify="space-between" align="middle" style={{ marginBottom: 16 }}>
        <Col>
          <Title level={2}>📊 {t('dashboard.title')}</Title>
        </Col>
        <Col>
          <Space>
            <Text>{t('dashboard.dateRange')}:</Text>
            <RangePicker
              value={dateRange}
              onChange={(dates) => {
                if (dates && dates[0] && dates[1]) {
                  setDateRange([dates[0], dates[1]]);
                }
              }}
            />
          </Space>
        </Col>
      </Row>

      {/* Overview Statistics */}
      <Row gutter={[16, 16]}>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title={t('dashboard.totalUsers')}
              value={statistics.totalUsers}
              prefix={<UserOutlined />}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title={t('dashboard.activeUsers24h')}
              value={statistics.activeUsers24h}
              prefix={<TeamOutlined />}
              valueStyle={{ color: "#3f8600" }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title={t('dashboard.totalLogins')}
              value={statistics.totalLogins}
              prefix={<LoginOutlined />}
            />
            <Divider style={{ margin: "12px 0" }} />
            <Row gutter={8}>
              <Col span={12}>
                <Text type="success">✓ {statistics.successfulLogins}</Text>
              </Col>
              <Col span={12}>
                <Text type="danger">✗ {statistics.failedLogins}</Text>
              </Col>
            </Row>
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title={t('dashboard.activeSessions')}
              value={statistics.activeSessions}
              prefix={<ApiOutlined />}
              valueStyle={{ color: "#1890ff" }}
            />
          </Card>
        </Col>
      </Row>

      {/* Success Rates */}
      <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
        <Col xs={24} sm={12}>
          <Card title={t('dashboard.loginSuccessRate')}>
            <Progress
              type="circle"
              percent={Math.round(statistics.loginSuccessRate)}
              format={(percent) => `${percent}%`}
              status={statistics.loginSuccessRate >= 95 ? "success" : "normal"}
            />
            <Divider />
            <Row>
              <Col span={12}>
                <Statistic
                  title={t('dashboard.successful')}
                  value={statistics.successfulLogins}
                  valueStyle={{ fontSize: 16, color: "#52c41a" }}
                />
              </Col>
              <Col span={12}>
                <Statistic
                  title={t('dashboard.failed')}
                  value={statistics.failedLogins}
                  valueStyle={{ fontSize: 16, color: "#ff4d4f" }}
                />
              </Col>
            </Row>
          </Card>
        </Col>
        <Col xs={24} sm={12}>
          <Card title={t('dashboard.apiCallStatistics')}>
            <Progress
              type="circle"
              percent={Math.round(100 - statistics.errorRate)}
              format={(percent) => `${percent}%`}
              status={statistics.errorRate <= 5 ? "success" : "exception"}
            />
            <Divider />
            <Row>
              <Col span={12}>
                <Statistic
                  title={t('dashboard.totalCalls')}
                  value={statistics.totalApiCalls}
                  valueStyle={{ fontSize: 16 }}
                />
              </Col>
              <Col span={12}>
                <Statistic
                  title={t('dashboard.errors')}
                  value={statistics.totalErrors}
                  valueStyle={{ fontSize: 16, color: "#ff4d4f" }}
                  prefix={<WarningOutlined />}
                />
              </Col>
            </Row>
          </Card>
        </Col>
      </Row>

      {/* Login Trends */}
      <Card
        title={t('dashboard.loginTrends')}
        style={{ marginTop: 16 }}
        extra={
          <Space>
            <Text>{t('dashboard.days')}:</Text>
            <a onClick={() => { setTrendDays(7); loadData(); }}>7</a>
            <a onClick={() => { setTrendDays(14); loadData(); }}>14</a>
            <a onClick={() => { setTrendDays(30); loadData(); }}>30</a>
          </Space>
        }
      >
        <Table
          dataSource={loginTrends}
          columns={loginTrendColumns}
          rowKey="date"
          pagination={false}
          size="small"
        />
      </Card>

      {/* API Call Statistics */}
      <Card title={t('dashboard.topApiCalls')} style={{ marginTop: 16 }}>
        <Table
          dataSource={apiStats}
          columns={apiStatsColumns}
          rowKey="action"
          pagination={false}
          size="small"
        />
      </Card>
    </div>
  );
}

