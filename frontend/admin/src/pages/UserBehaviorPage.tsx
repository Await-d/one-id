import { useState, useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import {
  Card,
  Row,
  Col,
  DatePicker,
  Space,
  Spin,
  Typography,
  Statistic,
  Table,
  Empty,
  Button,
  Switch,
  Tabs,
  message,
} from "antd";
import {
  MobileOutlined,
  DesktopOutlined,
  TabletOutlined,
  GlobalOutlined,
  UserOutlined,
  ApiOutlined,
  DownloadOutlined,
  ReloadOutlined,
  BarChartOutlined,
  PieChartOutlined,
} from "@ant-design/icons";
import {
  PieChart,
  Pie,
  Cell,
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  LineChart,
  Line,
} from "recharts";
import { userBehaviorApi, type UserBehaviorReport } from "../lib/userBehaviorApi";
import dayjs, { Dayjs } from "dayjs";

const { Title, Text } = Typography;
const { RangePicker } = DatePicker;

const COLORS = ["#0088FE", "#00C49F", "#FFBB28", "#FF8042", "#8884D8", "#82CA9D", "#FFC658", "#FF6B9D"];

export default function UserBehaviorPage() {
  const { t } = useTranslation();
  const [dateRange, setDateRange] = useState<[Dayjs, Dayjs]>([
    dayjs().subtract(30, "days"),
    dayjs(),
  ]);
  const [compareRange, setCompareRange] = useState<[Dayjs, Dayjs] | null>(null);
  const [autoRefresh, setAutoRefresh] = useState(false);
  const [viewMode, setViewMode] = useState<"table" | "chart">("chart");

  const { data: report, isLoading, refetch } = useQuery<UserBehaviorReport>({
    queryKey: ["user-behavior-report", dateRange[0].format("YYYY-MM-DD"), dateRange[1].format("YYYY-MM-DD")],
    queryFn: () =>
      userBehaviorApi.getBehaviorReport(dateRange[0].format("YYYY-MM-DD"), dateRange[1].format("YYYY-MM-DD")),
  });

  const { data: compareReport } = useQuery<UserBehaviorReport>({
    queryKey: [
      "user-behavior-report-compare",
      compareRange?.[0].format("YYYY-MM-DD"),
      compareRange?.[1].format("YYYY-MM-DD"),
    ],
    queryFn: () =>
      compareRange
        ? userBehaviorApi.getBehaviorReport(compareRange[0].format("YYYY-MM-DD"), compareRange[1].format("YYYY-MM-DD"))
        : Promise.resolve(null as any),
    enabled: !!compareRange,
  });

  // 自动刷新
  useEffect(() => {
    if (!autoRefresh) return;

    const interval = setInterval(() => {
      refetch();
      message.info(t("userBehavior.dataRefreshed"));
    }, 60000); // 每60秒刷新

    return () => clearInterval(interval);
  }, [autoRefresh, refetch, t]);

  const handleDateChange = (dates: [Dayjs | null, Dayjs | null] | null) => {
    if (dates && dates[0] && dates[1]) {
      setDateRange([dates[0], dates[1]]);
    }
  };

  const handleCompareChange = (dates: [Dayjs | null, Dayjs | null] | null) => {
    if (dates && dates[0] && dates[1]) {
      setCompareRange([dates[0], dates[1]]);
    } else {
      setCompareRange(null);
    }
  };

  // 导出 CSV
  const exportToCSV = () => {
    if (!report) return;

    const csvContent: string[] = [];

    // 添加标题
    csvContent.push("OneID User Behavior Analytics Report");
    csvContent.push(`Date Range: ${dayjs(report.startDate).format("YYYY-MM-DD")} to ${dayjs(report.endDate).format("YYYY-MM-DD")}`);
    csvContent.push(`Total Requests: ${report.totalRequests}`);
    csvContent.push(`Unique Users: ${report.uniqueUsers}`);
    csvContent.push("");

    // 设备类型
    csvContent.push("Device Types");
    csvContent.push("Device,Count,Percentage");
    Object.entries(report.deviceTypes).forEach(([device, count]) => {
      const percentage = ((count / report.totalRequests) * 100).toFixed(2);
      csvContent.push(`${device},${count},${percentage}%`);
    });
    csvContent.push("");

    // 浏览器
    csvContent.push("Browsers");
    csvContent.push("Browser,Count,Percentage");
    Object.entries(report.browsers).forEach(([browser, count]) => {
      const percentage = ((count / report.totalRequests) * 100).toFixed(2);
      csvContent.push(`${browser},${count},${percentage}%`);
    });
    csvContent.push("");

    // 操作系统
    csvContent.push("Operating Systems");
    csvContent.push("OS,Count,Percentage");
    Object.entries(report.operatingSystems).forEach(([os, count]) => {
      const percentage = ((count / report.totalRequests) * 100).toFixed(2);
      csvContent.push(`${os},${count},${percentage}%`);
    });
    csvContent.push("");

    // 地理位置
    csvContent.push("Geographic Distribution");
    csvContent.push("Region,Count,Percentage");
    Object.entries(report.countries).forEach(([region, count]) => {
      const percentage = ((count / report.totalRequests) * 100).toFixed(2);
      csvContent.push(`${region},${count},${percentage}%`);
    });

    const blob = new Blob([csvContent.join("\n")], { type: "text/csv;charset=utf-8;" });
    const link = document.createElement("a");
    const url = URL.createObjectURL(blob);
    link.setAttribute("href", url);
    link.setAttribute("download", `user-behavior-report-${dayjs().format("YYYY-MM-DD")}.csv`);
    link.style.visibility = "hidden";
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    message.success(t("userBehavior.exportSuccess"));
  };

  const convertToTableData = (data: Record<string, number>) => {
    return Object.entries(data).map(([key, value], index) => ({
      key: index,
      name: key,
      count: value,
      percentage: report ? ((value / report.totalRequests) * 100).toFixed(2) : "0",
    }));
  };

  const convertToChartData = (data: Record<string, number>) => {
    return Object.entries(data).map(([name, value]) => ({
      name,
      value,
      percentage: report ? ((value / report.totalRequests) * 100).toFixed(2) : "0",
    }));
  };

  const columns = [
    {
      title: t("userBehavior.name"),
      dataIndex: "name",
      key: "name",
    },
    {
      title: t("userBehavior.count"),
      dataIndex: "count",
      key: "count",
      sorter: (a: any, b: any) => b.count - a.count,
    },
    {
      title: t("userBehavior.percentage"),
      dataIndex: "percentage",
      key: "percentage",
      render: (value: string) => `${value}%`,
    },
  ];

  const renderPieChart = (data: Record<string, number>, title: string) => {
    const chartData = convertToChartData(data);
    if (chartData.length === 0) return <Empty description={t("common.noData")} />;

    return (
      <ResponsiveContainer width="100%" height={300}>
        <PieChart>
          <Pie
            data={chartData}
            cx="50%"
            cy="50%"
            labelLine={false}
            label={(entry) => `${entry.name}: ${entry.percentage}%`}
            outerRadius={80}
            fill="#8884d8"
            dataKey="value"
          >
            {chartData.map((entry, index) => (
              <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
            ))}
          </Pie>
          <Tooltip />
        </PieChart>
      </ResponsiveContainer>
    );
  };

  const renderBarChart = (data: Record<string, number>, compareData?: Record<string, number>) => {
    const chartData = Object.entries(data).map(([name, current]) => ({
      name,
      current,
      previous: compareData ? compareData[name] || 0 : undefined,
    }));

    if (chartData.length === 0) return <Empty description={t("common.noData")} />;

    return (
      <ResponsiveContainer width="100%" height={300}>
        <BarChart data={chartData}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="name" />
          <YAxis />
          <Tooltip />
          <Legend />
          <Bar dataKey="current" fill="#8884d8" name={t("userBehavior.currentPeriod")} />
          {compareData && (
            <Bar dataKey="previous" fill="#82ca9d" name={t("userBehavior.comparePeriod")} />
          )}
        </BarChart>
      </ResponsiveContainer>
    );
  };

  const tabItems = [
    {
      key: "devices",
      label: t("userBehavior.deviceTypes"),
      children: viewMode === "chart" ? (
        renderPieChart(report?.deviceTypes || {}, t("userBehavior.deviceTypes"))
      ) : (
        <Table dataSource={convertToTableData(report?.deviceTypes || {})} columns={columns} pagination={false} size="small" />
      ),
    },
    {
      key: "browsers",
      label: t("userBehavior.browsers"),
      children:
        viewMode === "chart" ? (
          renderBarChart(report?.browsers || {}, compareReport?.browsers)
        ) : (
          <Table dataSource={convertToTableData(report?.browsers || {})} columns={columns} pagination={false} size="small" />
        ),
    },
    {
      key: "os",
      label: t("userBehavior.operatingSystems"),
      children:
        viewMode === "chart" ? (
          renderBarChart(report?.operatingSystems || {}, compareReport?.operatingSystems)
        ) : (
          <Table
            dataSource={convertToTableData(report?.operatingSystems || {})}
            columns={columns}
            pagination={false}
            size="small"
          />
        ),
    },
    {
      key: "geo",
      label: t("userBehavior.geographic"),
      children:
        viewMode === "chart" ? (
          renderPieChart(report?.countries || {}, t("userBehavior.geographic"))
        ) : (
          <Table dataSource={convertToTableData(report?.countries || {})} columns={columns} pagination={false} size="small" />
        ),
    },
  ];

  return (
    <div style={{ padding: "24px" }}>
      <Card>
        <div style={{ marginBottom: 24 }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
            <div>
              <Title level={3} style={{ margin: 0, display: "flex", alignItems: "center", gap: 8 }}>
                <GlobalOutlined />
                {t("userBehavior.title")}
              </Title>
              <p style={{ margin: "8px 0 0 0", color: "#6b7280", fontSize: "14px", fontWeight: 400 }}>
                {t("userBehavior.subtitle")}
              </p>
            </div>
            <Space>
              <Switch
                checkedChildren={t("userBehavior.autoRefresh")}
                unCheckedChildren={t("userBehavior.autoRefresh")}
                checked={autoRefresh}
                onChange={setAutoRefresh}
              />
              <Button icon={<ReloadOutlined />} onClick={() => refetch()}>
                {t("common.refresh")}
              </Button>
              <Button icon={<DownloadOutlined />} onClick={exportToCSV} disabled={!report}>
                {t("common.export")} CSV
              </Button>
              <Button
                icon={viewMode === "chart" ? <BarChartOutlined /> : <PieChartOutlined />}
                onClick={() => setViewMode(viewMode === "chart" ? "table" : "chart")}
              >
                {viewMode === "chart" ? t("userBehavior.tableView") : t("userBehavior.chartView")}
              </Button>
            </Space>
          </div>
        </div>

        <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
          <Col span={12}>
            <Space direction="vertical" style={{ width: "100%" }}>
              <Text strong>{t("userBehavior.currentPeriod")}:</Text>
              <RangePicker value={dateRange} onChange={handleDateChange} format="YYYY-MM-DD" style={{ width: "100%" }} />
            </Space>
          </Col>
          <Col span={12}>
            <Space direction="vertical" style={{ width: "100%" }}>
              <Text strong>{t("userBehavior.comparePeriod")} ({t("common.optional")}):</Text>
              <RangePicker
                value={compareRange}
                onChange={handleCompareChange}
                format="YYYY-MM-DD"
                style={{ width: "100%" }}
                placeholder={[t("userBehavior.startDate"), t("userBehavior.endDate")]}
              />
            </Space>
          </Col>
        </Row>

        {isLoading ? (
          <div style={{ textAlign: "center", padding: "50px 0" }}>
            <Spin size="large" />
          </div>
        ) : report ? (
          <>
            {/* Summary Statistics */}
            <Row gutter={16} style={{ marginBottom: 24 }}>
              <Col span={6}>
                <Card>
                  <Statistic
                    title={t("userBehavior.totalRequests")}
                    value={report.totalRequests}
                    prefix={<ApiOutlined />}
                    suffix={
                      compareReport ? (
                        <Text type="secondary" style={{ fontSize: "14px" }}>
                          ({compareReport.totalRequests > report.totalRequests ? "▼" : "▲"}
                          {Math.abs(report.totalRequests - compareReport.totalRequests)})
                        </Text>
                      ) : null
                    }
                  />
                </Card>
              </Col>
              <Col span={6}>
                <Card>
                  <Statistic
                    title={t("userBehavior.uniqueUsers")}
                    value={report.uniqueUsers}
                    prefix={<UserOutlined />}
                    suffix={
                      compareReport ? (
                        <Text type="secondary" style={{ fontSize: "14px" }}>
                          ({compareReport.uniqueUsers > report.uniqueUsers ? "▼" : "▲"}
                          {Math.abs(report.uniqueUsers - compareReport.uniqueUsers)})
                        </Text>
                      ) : null
                    }
                  />
                </Card>
              </Col>
              <Col span={6}>
                <Card>
                  <Statistic
                    title={t("userBehavior.avgRequestsPerUser")}
                    value={report.uniqueUsers > 0 ? (report.totalRequests / report.uniqueUsers).toFixed(2) : 0}
                  />
                </Card>
              </Col>
              <Col span={6}>
                <Card>
                  <Statistic
                    title={t("userBehavior.dateRange")}
                    value={`${dayjs(report.startDate).format("MM/DD")} - ${dayjs(report.endDate).format("MM/DD")}`}
                  />
                </Card>
              </Col>
            </Row>

            {/* Charts/Tables */}
            <Tabs items={tabItems} />
          </>
        ) : (
          <Empty description={t("common.noData")} />
        )}
      </Card>
    </div>
  );
}
