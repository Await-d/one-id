import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Button,
  Card,
  Table,
  Tag,
  Space,
  Modal,
  Form,
  Input,
  InputNumber,
  Select,
  message,
  Popconfirm,
  Alert,
  Descriptions,
  Typography,
} from "antd";
import type { ColumnsType } from "antd/es/table";
import {
  PlusOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  DeleteOutlined,
  ThunderboltOutlined,
  ClearOutlined,
} from "@ant-design/icons";
import { useTranslation } from "react-i18next";
import { signingKeysApi, type SigningKey } from "../lib/signingKeysApi";
import dayjs from "dayjs";
import relativeTime from "dayjs/plugin/relativeTime";

dayjs.extend(relativeTime);

const { Title } = Typography;
const { TextArea } = Input;

export default function SigningKeysPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [includeRevoked, setIncludeRevoked] = useState(false);
  const [generateModalOpen, setGenerateModalOpen] = useState(false);
  const [revokeModalOpen, setRevokeModalOpen] = useState(false);
  const [cleanupModalOpen, setCleanupModalOpen] = useState(false);
  const [selectedKey, setSelectedKey] = useState<SigningKey | null>(null);
  const [keyType, setKeyType] = useState<"RSA" | "ECDSA">("RSA");
  const [form] = Form.useForm();
  const [revokeForm] = Form.useForm();
  const [cleanupForm] = Form.useForm();

  const { data: keys = [], isLoading } = useQuery({
    queryKey: ["signingKeys", includeRevoked],
    queryFn: () => signingKeysApi.getAll(includeRevoked),
  });

  const { data: rotationStatus } = useQuery({
    queryKey: ["rotationStatus", "RSA"],
    queryFn: () => signingKeysApi.getRotationStatus("RSA", 30),
    refetchInterval: 60000,
  });

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["signingKeys"] });
    queryClient.invalidateQueries({ queryKey: ["rotationStatus"] });
  };

  const generateMutation = useMutation({
    mutationFn: async (values: any) => {
      if (keyType === "RSA") {
        return await signingKeysApi.generateRsa({
          keySize: values.keySize,
          validityDays: values.validityDays,
          notes: values.notes,
        });
      } else {
        return await signingKeysApi.generateEcdsa({
          curve: values.curve,
          validityDays: values.validityDays,
          notes: values.notes,
        });
      }
    },
    onSuccess: () => {
      message.success(t("signingKeys.generateSuccess"));
      setGenerateModalOpen(false);
      form.resetFields();
      invalidate();
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || t("signingKeys.generateError"));
    },
  });

  const activateMutation = useMutation({
    mutationFn: (id: string) => signingKeysApi.activate(id),
    onSuccess: () => {
      message.success(t("signingKeys.activateSuccess"));
      invalidate();
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || t("signingKeys.activateError"));
    },
  });

  const revokeMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) =>
      signingKeysApi.revoke(id, { reason }),
    onSuccess: () => {
      message.success(t("signingKeys.revokeSuccess"));
      setRevokeModalOpen(false);
      revokeForm.resetFields();
      setSelectedKey(null);
      invalidate();
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || t("signingKeys.revokeError"));
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => signingKeysApi.delete(id),
    onSuccess: () => {
      message.success(t("signingKeys.deleteSuccess"));
      invalidate();
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || t("signingKeys.deleteError"));
    },
  });

  const cleanupMutation = useMutation({
    mutationFn: (retentionDays: number) => signingKeysApi.cleanup(retentionDays),
    onSuccess: (result) => {
      message.success(t("signingKeys.cleanupSuccess", { count: result.deletedCount }));
      setCleanupModalOpen(false);
      cleanupForm.resetFields();
      invalidate();
    },
    onError: (error: any) => {
      message.error(error.response?.data?.message || t("signingKeys.cleanupError"));
    },
  });

  const handleGenerate = () => {
    form.validateFields().then((values) => {
      generateMutation.mutate(values);
    });
  };

  const handleRevoke = () => {
    revokeForm.validateFields().then((values) => {
      if (selectedKey) {
        revokeMutation.mutate({ id: selectedKey.id, reason: values.reason });
      }
    });
  };

  const handleCleanup = () => {
    cleanupForm.validateFields().then((values) => {
      cleanupMutation.mutate(values.retentionDays);
    });
  };

  const columns: ColumnsType<SigningKey> = [
    {
      title: t("signingKeys.type"),
      dataIndex: "type",
      key: "type",
      width: 80,
      render: (type: string) => <Tag color={type === "RSA" ? "blue" : "green"}>{type}</Tag>,
    },
    {
      title: t("signingKeys.algorithm"),
      dataIndex: "algorithm",
      key: "algorithm",
      width: 100,
    },
    {
      title: t("signingKeys.version"),
      dataIndex: "version",
      key: "version",
      width: 80,
      render: (version: number) => <Tag>v{version}</Tag>,
    },
    {
      title: t("signingKeys.isActive"),
      dataIndex: "isActive",
      key: "isActive",
      width: 100,
      render: (isActive: boolean, record: SigningKey) => {
        if (record.revokedAt) {
          return (
            <Tag icon={<CloseCircleOutlined />} color="error">
              {t("signingKeys.revoked")}
            </Tag>
          );
        }
        if (isActive) {
          return (
            <Tag icon={<CheckCircleOutlined />} color="success">
              {t("signingKeys.active")}
            </Tag>
          );
        }
        return <Tag color="default">{t("signingKeys.inactive")}</Tag>;
      },
    },
    {
      title: t("signingKeys.createdAt"),
      dataIndex: "createdAt",
      key: "createdAt",
      width: 180,
      render: (date: string) => dayjs(date).format("YYYY-MM-DD HH:mm:ss"),
    },
    {
      title: t("signingKeys.expiresAt"),
      dataIndex: "expiresAt",
      key: "expiresAt",
      width: 180,
      render: (date: string) => {
        const isExpired = dayjs(date).isBefore(dayjs());
        return (
          <span style={{ color: isExpired ? "red" : undefined }}>
            {dayjs(date).format("YYYY-MM-DD HH:mm:ss")}
            {isExpired && ` (${t("signingKeys.expired")})`}
          </span>
        );
      },
    },
    {
      title: t("signingKeys.notes"),
      dataIndex: "notes",
      key: "notes",
      ellipsis: true,
    },
    {
      title: t("common.actions"),
      key: "action",
      width: 200,
      render: (_: any, record: SigningKey) => (
        <Space size="small">
          {!record.isActive && !record.revokedAt && (
            <Button
              type="link"
              size="small"
              onClick={() => activateMutation.mutate(record.id)}
              loading={activateMutation.isPending}
            >
              {t("signingKeys.activate")}
            </Button>
          )}
          {!record.revokedAt && (
            <Button
              type="link"
              size="small"
              danger
              onClick={() => {
                setSelectedKey(record);
                setRevokeModalOpen(true);
              }}
            >
              {t("signingKeys.revoke")}
            </Button>
          )}
          {!record.isActive && (
            <Popconfirm
              title={t("signingKeys.confirmDelete")}
              onConfirm={() => deleteMutation.mutate(record.id)}
              okText={t("common.confirm")}
              cancelText={t("common.cancel")}
            >
              <Button
                type="link"
                size="small"
                danger
                icon={<DeleteOutlined />}
                loading={deleteMutation.isPending}
              >
                {t("common.delete")}
              </Button>
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div style={{ padding: "24px" }}>
      <Card>
        <Space direction="vertical" size="large" style={{ width: "100%" }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
            <Title level={2}>{t("signingKeys.title")}</Title>
            <Space>
              <Button
                icon={<ClearOutlined />}
                onClick={() => setCleanupModalOpen(true)}
              >
                {t("signingKeys.cleanupExpired")}
              </Button>
              <Button
                type="primary"
                icon={<PlusOutlined />}
                onClick={() => setGenerateModalOpen(true)}
              >
                {t("signingKeys.generateKey")}
              </Button>
            </Space>
          </div>

          {rotationStatus?.shouldRotate && (
            <Alert
              message={t("signingKeys.rotationAlert")}
              description={
                rotationStatus.activeKey
                  ? t("signingKeys.rotationDescription", { 
                      date: dayjs(rotationStatus.activeKey.expiresAt).format("YYYY-MM-DD")
                    })
                  : t("signingKeys.rotationNoActive")
              }
              type="warning"
              showIcon
              icon={<ThunderboltOutlined />}
              closable
            />
          )}

          <div>
            <Space>
              <span>{t("signingKeys.displayOptions")}：</span>
              <Button
                type={!includeRevoked ? "primary" : "default"}
                onClick={() => setIncludeRevoked(false)}
              >
                {t("signingKeys.activeOnly")}
              </Button>
              <Button
                type={includeRevoked ? "primary" : "default"}
                onClick={() => setIncludeRevoked(true)}
              >
                {t("signingKeys.includeRevoked")}
              </Button>
            </Space>
          </div>

          <Table
            columns={columns}
            dataSource={keys}
            rowKey="id"
            loading={isLoading}
            pagination={{ pageSize: 10 }}
          />
        </Space>
      </Card>

      <Modal
        title={t("signingKeys.generateTitle")}
        open={generateModalOpen}
        onOk={handleGenerate}
        onCancel={() => {
          setGenerateModalOpen(false);
          form.resetFields();
        }}
        confirmLoading={generateMutation.isPending}
      >
        <Form form={form} layout="vertical" initialValues={{ keySize: 2048, validityDays: 90, curve: "P-256" }}>
          <Form.Item label={t("signingKeys.keyType")} name="keyType">
            <Select value={keyType} onChange={(value) => setKeyType(value as "RSA" | "ECDSA")}>
              <Select.Option value="RSA">RSA</Select.Option>
              <Select.Option value="ECDSA">{t("signingKeys.ecdsaType")}</Select.Option>
            </Select>
          </Form.Item>

          {keyType === "RSA" ? (
            <Form.Item
              label={t("signingKeys.keySize")}
              name="keySize"
              rules={[{ required: true, message: t("signingKeys.keySizeRequired") }]}
            >
              <Select>
                <Select.Option value={2048}>{t("signingKeys.keySize2048")}</Select.Option>
                <Select.Option value={3072}>{t("signingKeys.keySize3072")}</Select.Option>
                <Select.Option value={4096}>{t("signingKeys.keySize4096")}</Select.Option>
              </Select>
            </Form.Item>
          ) : (
            <Form.Item
              label={t("signingKeys.curve")}
              name="curve"
              rules={[{ required: true, message: t("signingKeys.curveRequired") }]}
            >
              <Select>
                <Select.Option value="P-256">{t("signingKeys.curveP256")}</Select.Option>
                <Select.Option value="P-384">{t("signingKeys.curveP384")}</Select.Option>
                <Select.Option value="P-521">{t("signingKeys.curveP521")}</Select.Option>
              </Select>
            </Form.Item>
          )}

          <Form.Item
            label={t("signingKeys.validityDays")}
            name="validityDays"
            rules={[{ required: true, message: t("signingKeys.validityDaysRequired") }]}
          >
            <InputNumber min={1} max={365} style={{ width: "100%" }} />
          </Form.Item>

          <Form.Item label={t("signingKeys.notes")} name="notes">
            <TextArea rows={3} placeholder={t("signingKeys.notesPlaceholder")} />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title={t("signingKeys.revokeTitle")}
        open={revokeModalOpen}
        onOk={handleRevoke}
        onCancel={() => {
          setRevokeModalOpen(false);
          revokeForm.resetFields();
          setSelectedKey(null);
        }}
        confirmLoading={revokeMutation.isPending}
      >
        {selectedKey && (
          <Space direction="vertical" size="large" style={{ width: "100%" }}>
            <Alert
              message={t("common.warning")}
              description={t("signingKeys.revokeWarning")}
              type="warning"
              showIcon
            />

            <Descriptions bordered size="small" column={1}>
              <Descriptions.Item label={t("signingKeys.type")}>{selectedKey.type}</Descriptions.Item>
              <Descriptions.Item label={t("signingKeys.algorithm")}>{selectedKey.algorithm}</Descriptions.Item>
              <Descriptions.Item label={t("signingKeys.version")}>v{selectedKey.version}</Descriptions.Item>
              <Descriptions.Item label={t("signingKeys.createdAt")}>
                {dayjs(selectedKey.createdAt).format("YYYY-MM-DD HH:mm:ss")}
              </Descriptions.Item>
            </Descriptions>

            <Form form={revokeForm} layout="vertical">
              <Form.Item label={t("signingKeys.revokeReason")} name="reason">
                <TextArea rows={3} placeholder={t("signingKeys.revokeReasonPlaceholder")} />
              </Form.Item>
            </Form>
          </Space>
        )}
      </Modal>

      <Modal
        title={t("signingKeys.cleanupTitle")}
        open={cleanupModalOpen}
        onOk={handleCleanup}
        onCancel={() => {
          setCleanupModalOpen(false);
          cleanupForm.resetFields();
        }}
        confirmLoading={cleanupMutation.isPending}
      >
        <Space direction="vertical" size="large" style={{ width: "100%" }}>
          <Alert
            message={t("signingKeys.cleanupTitle")}
            description={t("signingKeys.cleanupDescription")}
            type="info"
            showIcon
          />

          <Form form={cleanupForm} layout="vertical" initialValues={{ retentionDays: 30 }}>
            <Form.Item
              label={t("signingKeys.retentionDays")}
              name="retentionDays"
              rules={[{ required: true, message: t("signingKeys.retentionDaysRequired") }]}
              help={t("signingKeys.retentionDaysHelp")}
            >
              <InputNumber min={1} max={365} style={{ width: "100%" }} />
            </Form.Item>
          </Form>
        </Space>
      </Modal>
    </div>
  );
}
