import React from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Table, Button, message, Space, Popconfirm, Tag, Card, Descriptions } from "antd";
import { ArrowLeftOutlined, DeleteOutlined, LogoutOutlined } from "@ant-design/icons";
import { useTranslation } from "react-i18next";
import dayjs from "dayjs";
import relativeTime from "dayjs/plugin/relativeTime";

dayjs.extend(relativeTime);

interface UserSession {
  id: string;
  userId: string;
  ipAddress?: string;
  deviceInfo?: string;
  browserInfo?: string;
  osInfo?: string;
  location?: string;
  createdAt: string;
  lastActivityAt: string;
  expiresAt: string;
  isRevoked: boolean;
  revokedAt?: string;
  revokedReason?: string;
  isActive: boolean;
}

const UserSessionsPage: React.FC = () => {
  const { userId } = useParams<{ userId: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const { data: sessions, isLoading } = useQuery<UserSession[]>({
    queryKey: ["user-sessions", userId],
    queryFn: async () => {
      const response = await fetch(`/api/sessions/user/${userId}`, {
        headers: {
          Authorization: `Bearer ${localStorage.getItem("access_token")}`,
        },
      });
      if (!response.ok) throw new Error("Failed to fetch sessions");
      return response.json();
    },
    enabled: !!userId,
  });

  const revokeSessionMutation = useMutation({
    mutationFn: async ({ sessionId, reason }: { sessionId: string; reason: string }) => {
      const response = await fetch(`/api/sessions/${sessionId}/revoke`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${localStorage.getItem("access_token")}`,
        },
        body: JSON.stringify({ reason }),
      });
      if (!response.ok) throw new Error("Failed to revoke session");
    },
    onSuccess: () => {
      message.success(t("sessions.revokeSuccess"));
      queryClient.invalidateQueries({ queryKey: ["user-sessions", userId] });
    },
    onError: () => {
      message.error(t("sessions.revokeFailed"));
    },
  });

  const revokeAllMutation = useMutation({
    mutationFn: async (reason: string) => {
      const response = await fetch(`/api/sessions/user/${userId}/revoke-all`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${localStorage.getItem("access_token")}`,
        },
        body: JSON.stringify({ reason }),
      });
      if (!response.ok) throw new Error("Failed to revoke all sessions");
    },
    onSuccess: () => {
      message.success(t("sessions.revokeAllSuccess"));
      queryClient.invalidateQueries({ queryKey: ["user-sessions", userId] });
    },
    onError: () => {
      message.error(t("sessions.revokeAllFailed"));
    },
  });

  const handleRevokeSession = (sessionId: string) => {
    revokeSessionMutation.mutate({
      sessionId,
      reason: "Revoked by administrator",
    });
  };

  const handleRevokeAll = () => {
    revokeAllMutation.mutate("All sessions revoked by administrator");
  };

  const columns = [
    {
      title: t("sessions.device"),
      key: "device",
      render: (_: unknown, record: UserSession) => (
        <Space direction="vertical" size="small">
          <Space>
            <strong>{record.deviceInfo || "Unknown"}</strong>
            {record.isActive && <Tag color="green">{t("sessions.active")}</Tag>}
            {record.isRevoked && <Tag color="red">{t("sessions.revoked")}</Tag>}
          </Space>
          {record.browserInfo && (
            <span style={{ fontSize: "12px", color: "#666" }}>
              {record.browserInfo} on {record.osInfo || "Unknown OS"}
            </span>
          )}
        </Space>
      ),
    },
    {
      title: t("sessions.ipAddress"),
      dataIndex: "ipAddress",
      key: "ipAddress",
      render: (ip?: string) => ip || "-",
    },
    {
      title: t("sessions.location"),
      dataIndex: "location",
      key: "location",
      render: (location?: string) => location || "-",
    },
    {
      title: t("sessions.created"),
      dataIndex: "createdAt",
      key: "createdAt",
      render: (date: string) => dayjs(date).format("YYYY-MM-DD HH:mm:ss"),
    },
    {
      title: t("sessions.lastActivity"),
      dataIndex: "lastActivityAt",
      key: "lastActivityAt",
      render: (date: string) => dayjs(date).fromNow(),
    },
    {
      title: t("sessions.expires"),
      dataIndex: "expiresAt",
      key: "expiresAt",
      render: (date: string) => dayjs(date).format("YYYY-MM-DD HH:mm:ss"),
    },
    {
      title: t("common.actions"),
      key: "actions",
      render: (_: unknown, record: UserSession) => (
        <Popconfirm
          title={t("sessions.confirmRevoke")}
          onConfirm={() => handleRevokeSession(record.id)}
          disabled={record.isRevoked}
        >
          <Button
            type="link"
            danger
            icon={<DeleteOutlined />}
            disabled={record.isRevoked}
          >
            {t("sessions.revoke")}
          </Button>
        </Popconfirm>
      ),
    },
  ];

  const activeSessions = sessions?.filter((s) => s.isActive) || [];
  const revokedSessions = sessions?.filter((s) => s.isRevoked) || [];

  return (
    <div>
      <Card
        title={
          <Space>
            <Button
              type="text"
              icon={<ArrowLeftOutlined />}
              onClick={() => navigate("/users")}
            >
              {t("common.back")}
            </Button>
            <span>{t("sessions.title")}</span>
          </Space>
        }
        extra={
          <Popconfirm
            title={t("sessions.confirmRevokeAll")}
            onConfirm={handleRevokeAll}
            disabled={activeSessions.length === 0}
          >
            <Button
              danger
              icon={<LogoutOutlined />}
              disabled={activeSessions.length === 0}
            >
              {t("sessions.revokeAll")}
            </Button>
          </Popconfirm>
        }
      >
        <Descriptions bordered column={2} style={{ marginBottom: 16 }}>
          <Descriptions.Item label={t("sessions.totalSessions")}>
            {sessions?.length || 0}
          </Descriptions.Item>
          <Descriptions.Item label={t("sessions.activeSessions")}>
            <Tag color="green">{activeSessions.length}</Tag>
          </Descriptions.Item>
          <Descriptions.Item label={t("sessions.revokedSessions")}>
            <Tag color="red">{revokedSessions.length}</Tag>
          </Descriptions.Item>
        </Descriptions>

        <Table
          columns={columns}
          dataSource={sessions}
          rowKey="id"
          loading={isLoading}
          pagination={{ pageSize: 10 }}
        />
      </Card>
    </div>
  );
};

export default UserSessionsPage;

