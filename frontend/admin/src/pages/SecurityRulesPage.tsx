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
    Select,
    message,
    Popconfirm,
    Switch,
    Typography,
    Alert,
    Descriptions,
} from "antd";
import type { ColumnsType } from "antd/es/table";
import {
    PlusOutlined,
    CheckCircleOutlined,
    CloseCircleOutlined,
    DeleteOutlined,
    ExperimentOutlined,
} from "@ant-design/icons";
import { useTranslation } from "react-i18next";
import { securityRulesApi, type SecurityRule } from "../lib/securityRulesApi";
import dayjs from "dayjs";

const { Title, Text } = Typography;
const { TextArea } = Input;

export default function SecurityRulesPage() {
    const { t } = useTranslation();
    const queryClient = useQueryClient();
    const [includeDisabled, setIncludeDisabled] = useState(false);
    const [createModalOpen, setCreateModalOpen] = useState(false);
    const [editModalOpen, setEditModalOpen] = useState(false);
    const [testModalOpen, setTestModalOpen] = useState(false);
    const [selectedRule, setSelectedRule] = useState<SecurityRule | null>(null);
    const [form] = Form.useForm();
    const [editForm] = Form.useForm();
    const [testForm] = Form.useForm();
    const [testResult, setTestResult] = useState<any>(null);

    const { data: rules = [], isLoading } = useQuery({
        queryKey: ["securityRules", includeDisabled],
        queryFn: () => securityRulesApi.getAll(includeDisabled),
    });

    const invalidate = () => {
        queryClient.invalidateQueries({ queryKey: ["securityRules"] });
    };

    const createMutation = useMutation({
        mutationFn: (values: any) => securityRulesApi.create(values),
        onSuccess: () => {
            message.success(t("securityRules.createSuccess"));
            setCreateModalOpen(false);
            form.resetFields();
            invalidate();
        },
        onError: (error: any) => {
            message.error(error.response?.data?.message || t("securityRules.createError"));
        },
    });

    const updateMutation = useMutation({
        mutationFn: ({ id, values }: { id: string; values: any }) =>
            securityRulesApi.update(id, values),
        onSuccess: () => {
            message.success(t("securityRules.updateSuccess"));
            setEditModalOpen(false);
            editForm.resetFields();
            setSelectedRule(null);
            invalidate();
        },
        onError: (error: any) => {
            message.error(error.response?.data?.message || t("securityRules.updateError"));
        },
    });

    const toggleMutation = useMutation({
        mutationFn: ({ id, isEnabled }: { id: string; isEnabled: boolean }) =>
            securityRulesApi.toggle(id, isEnabled),
        onSuccess: () => {
            message.success(t("securityRules.toggleSuccess"));
            invalidate();
        },
        onError: (error: any) => {
            message.error(error.response?.data?.message || t("securityRules.toggleError"));
        },
    });

    const deleteMutation = useMutation({
        mutationFn: (id: string) => securityRulesApi.delete(id),
        onSuccess: () => {
            message.success(t("securityRules.deleteSuccess"));
            invalidate();
        },
        onError: (error: any) => {
            message.error(error.response?.data?.message || t("securityRules.deleteError"));
        },
    });

    const testMutation = useMutation({
        mutationFn: (ipAddress: string) => securityRulesApi.testIp(ipAddress),
        onSuccess: (result: any) => {
            setTestResult(result);
        },
        onError: (error: any) => {
            message.error(error.response?.data?.message || t("securityRules.testError"));
        },
    });

    const handleCreate = () => {
        form.validateFields().then((values) => {
            createMutation.mutate(values);
        });
    };

    const handleEdit = () => {
        editForm.validateFields().then((values) => {
            if (selectedRule) {
                updateMutation.mutate({ id: selectedRule.id, values });
            }
        });
    };

    const handleTest = () => {
        testForm.validateFields().then((values) => {
            testMutation.mutate(values.ipAddress);
        });
    };

    const columns: ColumnsType<SecurityRule> = [
        {
            title: t("securityRules.ruleType"),
            dataIndex: "ruleType",
            key: "ruleType",
            width: 150,
            render: (type: string) => {
                const colorMap: Record<string, string> = {
                    IpWhitelist: "green",
                    IpBlacklist: "red",
                    CountryBlock: "orange",
                    RegionBlock: "purple",
                };
                return <Tag color={colorMap[type]}>{t(`securityRules.types.${type}`)}</Tag>;
            },
        },
        {
            title: t("securityRules.ruleValue"),
            dataIndex: "ruleValue",
            key: "ruleValue",
            width: 200,
            render: (value: string) => <Text code>{value}</Text>,
        },
        {
            title: t("securityRules.description"),
            dataIndex: "description",
            key: "description",
            ellipsis: true,
        },
        {
            title: t("common.status"),
            dataIndex: "isEnabled",
            key: "isEnabled",
            width: 100,
            render: (isEnabled: boolean, record: SecurityRule) => (
                <Switch
                    checked={isEnabled}
                    onChange={(checked) => toggleMutation.mutate({ id: record.id, isEnabled: checked })}
                    checkedChildren={t("securityRules.enabled")}
                    unCheckedChildren={t("securityRules.disabled")}
                />
            ),
        },
        {
            title: t("securityRules.createdAt"),
            dataIndex: "createdAt",
            key: "createdAt",
            width: 180,
            render: (date: string) => dayjs(date).format("YYYY-MM-DD HH:mm:ss"),
        },
        {
            title: t("common.actions"),
            key: "action",
            width: 150,
            render: (_: any, record: SecurityRule) => (
                <Space size="small">
                    <Button
                        type="link"
                        size="small"
                        onClick={() => {
                            setSelectedRule(record);
                            editForm.setFieldsValue({
                                ruleValue: record.ruleValue,
                                description: record.description,
                            });
                            setEditModalOpen(true);
                        }}
                    >
                        {t("securityRules.edit")}
                    </Button>
                    <Popconfirm
                        title={t("securityRules.confirmDelete")}
                        onConfirm={() => deleteMutation.mutate(record.id)}
                        okText={t("common.confirm")}
                        cancelText={t("common.cancel")}
                    >
                        <Button type="link" size="small" danger icon={<DeleteOutlined />}>
                            {t("common.delete")}
                        </Button>
                    </Popconfirm>
                </Space>
            ),
        },
    ];

    return (
        <div style={{ padding: "24px" }}>
            <Card>
                <Space direction="vertical" size="large" style={{ width: "100%" }}>
                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                        <Title level={2}>{t("securityRules.title")}</Title>
                        <Space>
                            <Button icon={<ExperimentOutlined />} onClick={() => setTestModalOpen(true)}>
                                {t("securityRules.testIp")}
                            </Button>
                            <Button type="primary" icon={<PlusOutlined />} onClick={() => setCreateModalOpen(true)}>
                                {t("securityRules.createRule")}
                            </Button>
                        </Space>
                    </div>

                    <Alert
                        message={t("securityRules.infoTitle")}
                        description={
                            <div>
                                <p><strong>{t("securityRules.infoWhitelist")}</strong></p>
                                <p><strong>{t("securityRules.infoBlacklist")}</strong></p>
                                <p>{t("securityRules.infoCidr")}</p>
                            </div>
                        }
                        type="info"
                        showIcon
                    />

                    <div>
                        <Space>
                            <span>{t("securityRules.displayOptions")}：</span>
                            <Button type={!includeDisabled ? "primary" : "default"} onClick={() => setIncludeDisabled(false)}>
                                {t("securityRules.enabledOnly")}
                            </Button>
                            <Button type={includeDisabled ? "primary" : "default"} onClick={() => setIncludeDisabled(true)}>
                                {t("securityRules.showAll")}
                            </Button>
                        </Space>
                    </div>

                    <Table
                        columns={columns}
                        dataSource={rules}
                        rowKey="id"
                        loading={isLoading}
                        pagination={{ pageSize: 10 }}
                    />
                </Space>
            </Card>

            <Modal
                title={t("securityRules.createTitle")}
                open={createModalOpen}
                onOk={handleCreate}
                onCancel={() => {
                    setCreateModalOpen(false);
                    form.resetFields();
                }}
                confirmLoading={createMutation.isPending}
            >
                <Form form={form} layout="vertical">
                    <Form.Item 
                        label={t("securityRules.ruleType")} 
                        name="ruleType" 
                        rules={[{ required: true, message: t("securityRules.ruleTypeRequired") }]}
                    >
                        <Select>
                            <Select.Option value="IpWhitelist">{t("securityRules.types.IpWhitelist")}</Select.Option>
                            <Select.Option value="IpBlacklist">{t("securityRules.types.IpBlacklist")}</Select.Option>
                            <Select.Option value="CountryBlock">{t("securityRules.types.CountryBlock")}</Select.Option>
                            <Select.Option value="RegionBlock">{t("securityRules.types.RegionBlock")}</Select.Option>
                        </Select>
                    </Form.Item>

                    <Form.Item
                        label={t("securityRules.ruleValue")}
                        name="ruleValue"
                        rules={[{ required: true, message: t("securityRules.ruleValueRequired") }]}
                        help={t("securityRules.ruleValueHelp")}
                    >
                        <Input placeholder={t("securityRules.ruleValuePlaceholder")} />
                    </Form.Item>

                    <Form.Item label={t("securityRules.description")} name="description">
                        <TextArea rows={3} placeholder={t("securityRules.descriptionPlaceholder")} />
                    </Form.Item>
                </Form>
            </Modal>

            <Modal
                title={t("securityRules.editTitle")}
                open={editModalOpen}
                onOk={handleEdit}
                onCancel={() => {
                    setEditModalOpen(false);
                    editForm.resetFields();
                    setSelectedRule(null);
                }}
                confirmLoading={updateMutation.isPending}
            >
                {selectedRule && (
                    <Space direction="vertical" size="large" style={{ width: "100%" }}>
                        <Descriptions bordered size="small" column={1}>
                            <Descriptions.Item label={t("securityRules.ruleType")}>
                                {t(`securityRules.types.${selectedRule.ruleType}`)}
                            </Descriptions.Item>
                        </Descriptions>

                        <Form form={editForm} layout="vertical">
                            <Form.Item 
                                label={t("securityRules.ruleValue")} 
                                name="ruleValue" 
                                rules={[{ required: true, message: t("securityRules.ruleValueRequired") }]}
                            >
                                <Input placeholder={t("securityRules.ruleValuePlaceholder")} />
                            </Form.Item>

                            <Form.Item label={t("securityRules.description")} name="description">
                                <TextArea rows={3} placeholder={t("securityRules.descriptionPlaceholder")} />
                            </Form.Item>
                        </Form>
                    </Space>
                )}
            </Modal>

            <Modal
                title={t("securityRules.testTitle")}
                open={testModalOpen}
                onOk={handleTest}
                onCancel={() => {
                    setTestModalOpen(false);
                    testForm.resetFields();
                    setTestResult(null);
                }}
                confirmLoading={testMutation.isPending}
            >
                <Space direction="vertical" size="large" style={{ width: "100%" }}>
                    <Form form={testForm} layout="vertical">
                        <Form.Item
                            label={t("securityRules.testIpAddress")}
                            name="ipAddress"
                            rules={[{ required: true, message: t("securityRules.testIpRequired") }]}
                        >
                            <Input placeholder={t("securityRules.testIpPlaceholder")} />
                        </Form.Item>
                    </Form>

                    {testResult && (
                        <Alert
                            message={t("securityRules.testResult")}
                            description={
                                <div>
                                    <p><strong>{t("securityRules.testIpAddress")}</strong>: {testResult.ipAddress}</p>
                                    <p>
                                        <strong>{t("securityRules.testIsAllowed")}</strong>:{" "}
                                        {testResult.isAllowed ? (
                                            <Tag icon={<CheckCircleOutlined />} color="success">
                                                {t("securityRules.testAllowed")}
                                            </Tag>
                                        ) : (
                                            <Tag icon={<CloseCircleOutlined />} color="error">
                                                {t("securityRules.testDenied")}
                                            </Tag>
                                        )}
                                    </p>
                                </div>
                            }
                            type={testResult.isAllowed ? "success" : "error"}
                            showIcon
                        />
                    )}
                </Space>
            </Modal>
        </div>
    );
}
