import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import {
  Card,
  Table,
  Tag,
  Button,
  DatePicker,
  Space,
  Typography,
  Badge,
  Tooltip,
  message,
} from "antd";
import type { ColumnsType } from "antd/es/table";
import {
  WarningOutlined,
  CheckOutlined,
  GlobalOutlined,
  ClockCircleOutlined,
} from "@ant-design/icons";
import { anomalyDetectionApi, type LoginHistory } from "../lib/anomalyDetectionApi";
import dayjs, { Dayjs } from "dayjs";

const { Title } = Typography;
const { RangePicker } = DatePicker;

export default function AnomalousLoginsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [dateRange, setDateRange] = useState<[Dayjs, Dayjs]>([
    dayjs().subtract(7, "days"),
    dayjs(),
  ]);

  const { data: anomalousLogins = [], isLoading } = useQuery<LoginHistory[]>({
    queryKey: ["anomalous-logins", dateRange[0].format("YYYY-MM-DD"), dateRange[1].format("YYYY-MM-DD")],
    queryFn: () =>
      anomalyDetectionApi.getAllAnomalousLogins(
        dateRange[0].format("YYYY-MM-DD"),
        dateRange[1].format("YYYY-MM-DD")
      ),
  });

  const markNotifiedMutation = useMutation({
    mutationFn: (id: string) => anomalyDetectionApi.markAsNotified(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["anomalous-logins"] });
      message.success(t("anomalousLogins.markedAsNotified"));
    },
  });

  const getRiskLevel = (score: number) => {
    if (score >= 70) return { text: t("anomalousLogins.highRisk"), color: "red" };
    if (score >= 40) return { text: t("anomalousLogins.mediumRisk"), color: "orange" };
    return { text: t("anomalousLogins.lowRisk"), color: "yellow" };
  };

  const columns: ColumnsType<LoginHistory> = [
    {
      title: t("anomalousLogins.riskScore"),
      dataIndex: "riskScore",
      key: "riskScore",
      width: 100,
      sorter: (a, b) => b.riskScore - a.riskScore,
      render: (score: number) => {
        const { text, color } = getRiskLevel(score);
        return (
          <Badge count={score} style={{ backgroundColor: color === "red" ? "#f5222d" : color === "orange" ? "#fa8c16" : "#faad14" }}>
            <Tag color={color}>{text}</Tag>
          </Badge>
        );
      },
    },
    {
      title: t("anomalousLogins.user"),
      dataIndex: "userName",
      key: "userName",
      width: 150,
    },
    {
      title: t("anomalousLogins.loginTime"),
      dataIndex: "loginTime",
      key: "loginTime",
      width: 180,
      render: (time: string) => dayjs(time).format("YYYY-MM-DD HH:mm:ss"),
    },
    {
      title: t("anomalousLogins.location"),
      key: "location",
      width: 150,
      render: (_, record) => (
        <Space>
          <GlobalOutlined />
          {record.country || t("anomalousLogins.unknown")}
          {record.city && ` (${record.city})`}
        </Space>
      ),
    },
    {
      title: t("anomalousLogins.ipAddress"),
      dataIndex: "ipAddress",
      key: "ipAddress",
      width: 130,
    },
    {
      title: t("anomalousLogins.deviceInfo"),
      key: "device",
      width: 200,
      render: (_, record) => (
        <div>
          <div>{record.browser || t("anomalousLogins.unknown")}</div>
          <div style={{ fontSize: "12px", color: "#888" }}>
            {record.operatingSystem || t("anomalousLogins.unknown")} • {record.deviceType || t("anomalousLogins.unknown")}
          </div>
        </div>
      ),
    },
    {
      title: t("anomalousLogins.anomalyReason"),
      dataIndex: "anomalyReason",
      key: "anomalyReason",
      ellipsis: true,
      render: (reason: string) => (
        <Tooltip title={reason}>
          <span style={{ color: "#ff4d4f" }}>
            <WarningOutlined /> {reason}
          </span>
        </Tooltip>
      ),
    },
    {
      title: t("common.status"),
      dataIndex: "userNotified",
      key: "userNotified",
      width: 100,
      render: (notified: boolean) =>
        notified ? (
          <Tag color="green" icon={<CheckOutlined />}>
            {t("anomalousLogins.notified")}
          </Tag>
        ) : (
          <Tag color="orange" icon={<ClockCircleOutlined />}>
            {t("anomalousLogins.pendingNotification")}
          </Tag>
        ),
    },
    {
      title: t("common.actions"),
      key: "actions",
      width: 120,
      fixed: "right",
      render: (_, record) => (
        <Space>
          {!record.userNotified && (
            <Button
              size="small"
              type="primary"
              onClick={() => markNotifiedMutation.mutate(record.id)}
              loading={markNotifiedMutation.isPending}
            >
              {t("anomalousLogins.markNotified")}
            </Button>
          )}
        </Space>
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
                <WarningOutlined style={{ color: "#ff4d4f" }} />
                {t("anomalousLogins.title")}
              </Title>
              <p style={{ margin: "8px 0 0 0", color: "#6b7280", fontSize: "14px", fontWeight: 400 }}>
                {t("anomalousLogins.subtitle")}
              </p>
            </div>
            <Space>
              <RangePicker
                value={dateRange}
                onChange={(dates) => dates && dates[0] && dates[1] && setDateRange([dates[0], dates[1]])}
                format="YYYY-MM-DD"
              />
            </Space>
          </div>
        </div>

        <div style={{ marginBottom: 16 }}>
          <Space size="large">
            <div>
              <span style={{ fontSize: "14px", color: "#888" }}>{t("anomalousLogins.totalAnomalous")}：</span>
              <span style={{ fontSize: "24px", fontWeight: "bold", color: "#ff4d4f", marginLeft: 8 }}>
                {anomalousLogins.length}
              </span>
            </div>
            <div>
              <span style={{ fontSize: "14px", color: "#888" }}>{t("anomalousLogins.pending")}：</span>
              <span style={{ fontSize: "24px", fontWeight: "bold", color: "#fa8c16", marginLeft: 8 }}>
                {anomalousLogins.filter((l) => !l.userNotified).length}
              </span>
            </div>
            <div>
              <span style={{ fontSize: "14px", color: "#888" }}>{t("anomalousLogins.highRisk")}：</span>
              <span style={{ fontSize: "24px", fontWeight: "bold", color: "#f5222d", marginLeft: 8 }}>
                {anomalousLogins.filter((l) => l.riskScore >= 70).length}
              </span>
            </div>
          </Space>
        </div>

        <Table
          rowKey="id"
          columns={columns}
          dataSource={anomalousLogins}
          loading={isLoading}
          pagination={{ pageSize: 20 }}
          scroll={{ x: 1400 }}
        />
      </Card>
    </div>
  );
}

