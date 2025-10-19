import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import {
    Card,
    Form,
    Input,
    Button,
    Space,
    Typography,
    Alert,
    Radio,
    message,
    Divider,
    Descriptions,
} from "antd";
import {
    DownloadOutlined,
    DeleteOutlined,
    ExclamationCircleOutlined,
    SafetyOutlined,
} from "@ant-design/icons";
import { useTranslation } from "react-i18next";
import { gdprApi } from "../lib/gdprApi";

const { Title, Text, Paragraph } = Typography;

export default function GdprPage() {
    const { t } = useTranslation();
    const [exportForm] = Form.useForm();
    const [deleteForm] = Form.useForm();
    const [deleteType, setDeleteType] = useState<"soft" | "hard">("soft");

    const exportMutation = useMutation({
        mutationFn: async (userId: string) => {
            const blob = await gdprApi.exportUserData(userId);

            const url = window.URL.createObjectURL(blob);
            const a = document.createElement("a");
            a.href = url;
            a.download = `user_data_${userId}_${new Date().toISOString()}.json`;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);

            return userId;
        },
        onSuccess: (userId) => {
            message.success(t("gdpr.exportSuccess", { userId }));
            exportForm.resetFields();
        },
        onError: (error: any) => {
            message.error(error.message || t("gdpr.exportError"));
        },
    });

    const deleteMutation = useMutation({
        mutationFn: ({ userId, softDelete }: { userId: string; softDelete: boolean }) =>
            gdprApi.deleteUserData(userId, softDelete),
        onSuccess: (_, variables) => {
            message.success(
                variables.softDelete
                    ? t("gdpr.anonymizeSuccess")
                    : t("gdpr.permanentDeleteSuccess")
            );
            deleteForm.resetFields();
        },
        onError: (error: any) => {
            message.error(error.response?.data?.message || t("gdpr.deleteError"));
        },
    });

    const handleExport = () => {
        exportForm.validateFields().then((values) => {
            exportMutation.mutate(values.userId);
        });
    };

    const handleDelete = () => {
        deleteForm.validateFields().then((values) => {
            deleteMutation.mutate({
                userId: values.userId,
                softDelete: deleteType === "soft",
            });
        });
    };

    return (
        <div style={{ padding: "24px" }}>
            <Space direction="vertical" size="large" style={{ width: "100%" }}>
                <Card>
                    <Space direction="vertical" size="middle" style={{ width: "100%" }}>
                        <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
                            <SafetyOutlined style={{ fontSize: "24px", color: "#1890ff" }} />
                            <Title level={2} style={{ margin: 0 }}>
                                {t("gdpr.title")}
                            </Title>
                        </div>

                        <Alert
                            message={t("gdpr.complianceTitle")}
                            description={
                                <div>
                                    <p>{t("gdpr.rightsDescription")}</p>
                                    <ul>
                                        <li><strong>{t("gdpr.rightPortability")}</strong></li>
                                        <li><strong>{t("gdpr.rightErasure")}</strong></li>
                                    </ul>
                                    <p style={{ marginTop: "8px", color: "#ff4d4f" }}>
                                        <ExclamationCircleOutlined /> {t("gdpr.auditNote")}
                                    </p>
                                </div>
                            }
                            type="info"
                            showIcon
                        />
                    </Space>
                </Card>

                <Card
                    title={
                        <Space>
                            <DownloadOutlined />
                            <span>{t("gdpr.exportTitle")}</span>
                        </Space>
                    }
                >
                    <Space direction="vertical" size="large" style={{ width: "100%" }}>
                        <Paragraph>
                            {t("gdpr.exportDescription")}
                        </Paragraph>
                        <ul>
                            <li>{t("gdpr.exportBasicInfo")}</li>
                            <li>{t("gdpr.exportRoles")}</li>
                            <li>{t("gdpr.exportClaims")}</li>
                            <li>{t("gdpr.exportLogins")}</li>
                            <li>{t("gdpr.exportApiKeys")}</li>
                            <li>{t("gdpr.exportSessions")}</li>
                            <li>{t("gdpr.exportAuditLogs")}</li>
                        </ul>

                        <Form form={exportForm} layout="inline">
                            <Form.Item
                                name="userId"
                                rules={[{ required: true, message: t("gdpr.exportUserIdRequired") }]}
                                style={{ flex: 1 }}
                            >
                                <Input
                                    placeholder={t("gdpr.exportUserIdPlaceholder")}
                                    prefix={<DownloadOutlined />}
                                />
                            </Form.Item>
                            <Form.Item>
                                <Button
                                    type="primary"
                                    onClick={handleExport}
                                    loading={exportMutation.isPending}
                                    icon={<DownloadOutlined />}
                                >
                                    {t("gdpr.exportButton")}
                                </Button>
                            </Form.Item>
                        </Form>

                        <Alert
                            message={t("gdpr.exportFormatTitle")}
                            description={t("gdpr.exportFormatDescription")}
                            type="success"
                            showIcon
                        />
                    </Space>
                </Card>

                <Card
                    title={
                        <Space>
                            <DeleteOutlined />
                            <span>{t("gdpr.deleteTitle")}</span>
                        </Space>
                    }
                >
                    <Space direction="vertical" size="large" style={{ width: "100%" }}>
                        <Alert
                            message={t("common.warning")}
                            description={t("gdpr.deleteWarning")}
                            type="warning"
                            showIcon
                            icon={<ExclamationCircleOutlined />}
                        />

                        <Divider orientation="left">{t("gdpr.deleteTypeTitle")}</Divider>

                        <Radio.Group value={deleteType} onChange={(e) => setDeleteType(e.target.value)}>
                            <Space direction="vertical" size="middle">
                                <Radio value="soft">
                                    <Space direction="vertical" size="small">
                                        <Text strong>{t("gdpr.deleteSoft")}</Text>
                                        <Text type="secondary" style={{ fontSize: "12px" }}>
                                            • {t("gdpr.deleteSoftInfo1")}<br />
                                            • {t("gdpr.deleteSoftInfo2")}<br />
                                            • {t("gdpr.deleteSoftInfo3")}<br />
                                            • {t("gdpr.deleteSoftInfo4")}<br />
                                            • {t("gdpr.deleteSoftInfo5")}
                                        </Text>
                                    </Space>
                                </Radio>
                                <Radio value="hard">
                                    <Space direction="vertical" size="small">
                                        <Text strong style={{ color: "#ff4d4f" }}>
                                            {t("gdpr.deleteHard")}
                                        </Text>
                                        <Text type="secondary" style={{ fontSize: "12px" }}>
                                            • {t("gdpr.deleteHardInfo1")}<br />
                                            • {t("gdpr.deleteHardInfo2")}<br />
                                            • {t("gdpr.deleteHardInfo3")}<br />
                                            • {t("gdpr.deleteHardInfo4")}
                                        </Text>
                                    </Space>
                                </Radio>
                            </Space>
                        </Radio.Group>

                        <Divider />

                        <Form form={deleteForm} layout="inline">
                            <Form.Item
                                name="userId"
                                rules={[{ required: true, message: t("gdpr.deleteUserIdRequired") }]}
                                style={{ flex: 1 }}
                            >
                                <Input
                                    placeholder={t("gdpr.deleteUserIdPlaceholder")}
                                    prefix={<DeleteOutlined />}
                                />
                            </Form.Item>
                            <Form.Item>
                                <Button
                                    danger
                                    onClick={handleDelete}
                                    loading={deleteMutation.isPending}
                                    icon={<DeleteOutlined />}
                                >
                                    {deleteType === "soft" ? t("gdpr.anonymizeButton") : t("gdpr.permanentDeleteButton")}
                                </Button>
                            </Form.Item>
                        </Form>
                    </Space>
                </Card>

                <Card title={t("gdpr.complianceInfoTitle")}>
                    <Descriptions bordered column={1} size="small">
                        <Descriptions.Item label={t("gdpr.rightPortabilityArticle")}>
                            {t("gdpr.rightPortabilityDesc")}
                        </Descriptions.Item>
                        <Descriptions.Item label={t("gdpr.rightErasureArticle")}>
                            {t("gdpr.rightErasureDesc")}
                        </Descriptions.Item>
                        <Descriptions.Item label={t("gdpr.auditTrail")}>
                            {t("gdpr.auditTrailDesc")}
                        </Descriptions.Item>
                        <Descriptions.Item label={t("gdpr.responseTime")}>
                            {t("gdpr.responseTimeDesc")}
                        </Descriptions.Item>
                    </Descriptions>
                </Card>
            </Space>
        </div>
    );
}
