import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import {
  Card,
  Table,
  Button,
  Form,
  Input,
  Modal,
  message,
  Space,
  Tag,
  Popconfirm,
  Typography,
  Select,
  Switch,
  Tabs,
  Alert,
} from "antd";
import type { ColumnsType } from "antd/es/table";
import {
  MailOutlined,
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  CopyOutlined,
  CheckCircleOutlined,
} from "@ant-design/icons";
import { emailTemplatesApi, type EmailTemplate } from "../lib/emailTemplatesApi";
import dayjs from "dayjs";

const { Title, Text } = Typography;
const { TextArea } = Input;

export default function EmailTemplatesPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [modalOpen, setModalOpen] = useState(false);
  const [editingTemplate, setEditingTemplate] = useState<EmailTemplate | null>(null);
  const [selectedLanguage, setSelectedLanguage] = useState<string>("en");
  const [form] = Form.useForm();

  const { data: templates = [], isLoading } = useQuery<EmailTemplate[]>({
    queryKey: ["email-templates"],
    queryFn: () => emailTemplatesApi.getAllTemplates(),
  });

  const createMutation = useMutation({
    mutationFn: (data: Partial<EmailTemplate>) => emailTemplatesApi.createTemplate(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["email-templates"] });
      setModalOpen(false);
      form.resetFields();
      message.success(t("emailTemplates.createSuccess"));
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<EmailTemplate> }) =>
      emailTemplatesApi.updateTemplate(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["email-templates"] });
      setModalOpen(false);
      form.resetFields();
      setEditingTemplate(null);
      message.success(t("emailTemplates.updateSuccess"));
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => emailTemplatesApi.deleteTemplate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["email-templates"] });
      message.success(t("emailTemplates.deleteSuccess"));
    },
  });

  const duplicateMutation = useMutation({
    mutationFn: ({ id, targetLanguage }: { id: string; targetLanguage: string }) =>
      emailTemplatesApi.duplicateTemplate(id, targetLanguage),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["email-templates"] });
      message.success(t("emailTemplates.duplicateSuccess"));
    },
  });

  const initDefaultsMutation = useMutation({
    mutationFn: () => emailTemplatesApi.ensureDefaults(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["email-templates"] });
      message.success(t("emailTemplates.initSuccess"));
    },
  });

  const handleCreate = () => {
    setEditingTemplate(null);
    form.resetFields();
    setModalOpen(true);
  };

  const handleEdit = (template: EmailTemplate) => {
    setEditingTemplate(template);
    form.setFieldsValue(template);
    setModalOpen(true);
  };

  const handleDelete = (id: string) => {
    deleteMutation.mutate(id);
  };

  const handleDuplicate = (id: string) => {
    Modal.confirm({
      title: t("emailTemplates.duplicate"),
      content: (
        <Form>
          <Form.Item label={t("emailTemplates.language")} name="targetLanguage">
            <Select>
              <Select.Option value="en">{t("emailTemplates.languages.en")}</Select.Option>
              <Select.Option value="zh">{t("emailTemplates.languages.zh")}</Select.Option>
              <Select.Option value="ja">{t("emailTemplates.languages.ja")}</Select.Option>
            </Select>
          </Form.Item>
        </Form>
      ),
      onOk: (close) => {
        const targetLanguage = form.getFieldValue("targetLanguage");
        if (!targetLanguage) {
          message.error(t("emailTemplates.targetLanguageRequired"));
          return;
        }
        duplicateMutation.mutate({ id, targetLanguage });
        close();
      },
    });
  };

  const handleSave = () => {
    form.validateFields().then((values) => {
      if (editingTemplate) {
        updateMutation.mutate({ id: editingTemplate.id, data: values });
      } else {
        createMutation.mutate(values);
      }
    });
  };

  const handleInitDefaults = () => {
    Modal.confirm({
      title: t("emailTemplates.initDefaults"),
      content: t("emailTemplates.confirmInitDefaults"),
      onOk: () => initDefaultsMutation.mutate(),
    });
  };

  const filteredTemplates = selectedLanguage
    ? templates.filter((t) => t.language === selectedLanguage)
    : templates;

  const columns: ColumnsType<EmailTemplate> = [
    {
      title: t("emailTemplates.templateType"),
      dataIndex: "templateKey",
      key: "templateKey",
      render: (type: string) => (
        <Tag color="blue">{type}</Tag>
      ),
    },
    {
      title: t("emailTemplates.language"),
      dataIndex: "language",
      key: "language",
      render: (lang: string) => (
        <Tag color="green">{t(`emailTemplates.languages.${lang}`) || lang}</Tag>
      ),
    },
    {
      title: t("emailTemplates.subject"),
      dataIndex: "subject",
      key: "subject",
      ellipsis: true,
    },
    {
      title: t("emailTemplates.isDefault"),
      dataIndex: "isDefault",
      key: "isDefault",
      render: (isDefault: boolean) =>
        isDefault ? (
          <CheckCircleOutlined style={{ color: "#52c41a", fontSize: 18 }} />
        ) : null,
    },
    {
      title: t("emailTemplates.updatedAt"),
      dataIndex: "updatedAt",
      key: "updatedAt",
      render: (date: string, record: EmailTemplate) => {
        const displayDate = date || record.createdAt;
        return dayjs(displayDate).format("YYYY-MM-DD HH:mm");
      },
    },
    {
      title: t("common.actions"),
      key: "actions",
      render: (_, record) => (
        <Space>
          <Button
            icon={<EditOutlined />}
            size="small"
            onClick={() => handleEdit(record)}
          >
            {t("common.edit")}
          </Button>
          <Button
            icon={<CopyOutlined />}
            size="small"
            onClick={() => handleDuplicate(record.id)}
          >
            {t("emailTemplates.duplicate")}
          </Button>
          <Popconfirm
            title={t("emailTemplates.confirmDelete")}
            onConfirm={() => handleDelete(record.id)}
          >
            <Button icon={<DeleteOutlined />} size="small" danger>
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
        <div style={{ marginBottom: 24 }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
            <div>
              <Title level={3} style={{ margin: 0, display: "flex", alignItems: "center", gap: 8 }}>
                <MailOutlined />
                {t("emailTemplates.title")}
              </Title>
              <p style={{ margin: "8px 0 0 0", color: "#6b7280", fontSize: "14px", fontWeight: 400 }}>
                {t("emailTemplates.subtitle")}
              </p>
            </div>
            <Space>
              <Button icon={<PlusOutlined />} type="primary" onClick={handleCreate}>
                {t("emailTemplates.create")}
              </Button>
              <Button onClick={handleInitDefaults}>
                {t("emailTemplates.initDefaults")}
              </Button>
            </Space>
          </div>
        </div>

        <Alert
          message={t("emailTemplates.availableVariables")}
          description={t("emailTemplates.variablesHelp")}
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
        />

        <div style={{ marginBottom: 16 }}>
          <Select
            style={{ width: 200 }}
            placeholder={t("emailTemplates.selectLanguage")}
            allowClear
            value={selectedLanguage}
            onChange={setSelectedLanguage}
          >
            <Select.Option value="">{t("emailTemplates.allLanguages")}</Select.Option>
            <Select.Option value="en">{t("emailTemplates.languages.en")}</Select.Option>
            <Select.Option value="zh">{t("emailTemplates.languages.zh")}</Select.Option>
            <Select.Option value="ja">{t("emailTemplates.languages.ja")}</Select.Option>
          </Select>
        </div>

        <Table
          rowKey="id"
          columns={columns}
          dataSource={filteredTemplates}
          loading={isLoading}
          pagination={{ pageSize: 10 }}
        />

        <Modal
          title={editingTemplate ? t("emailTemplates.editTitle") : t("emailTemplates.createTitle")}
          open={modalOpen}
          onCancel={() => {
            setModalOpen(false);
            form.resetFields();
            setEditingTemplate(null);
          }}
          onOk={handleSave}
          width={800}
          okText={t("common.save")}
          cancelText={t("common.cancel")}
        >
          <Form form={form} layout="vertical">
            <Form.Item
              label={t("emailTemplates.templateType")}
              name="templateKey"
              rules={[{ required: true, message: t("emailTemplates.templateTypeRequired") }]}
            >
              <Input placeholder={t("emailTemplates.templateTypeRequired")} />
            </Form.Item>

            <Form.Item
              label="Template Name"
              name="name"
              rules={[{ required: true, message: "Please enter template name" }]}
            >
              <Input placeholder="Enter template name" />
            </Form.Item>

            <Form.Item
              label={t("emailTemplates.language")}
              name="language"
              rules={[{ required: true, message: t("emailTemplates.languageRequired") }]}
            >
              <Select placeholder={t("emailTemplates.languageRequired")}>
                <Select.Option value="en">{t("emailTemplates.languages.en")}</Select.Option>
                <Select.Option value="zh">{t("emailTemplates.languages.zh")}</Select.Option>
                <Select.Option value="ja">{t("emailTemplates.languages.ja")}</Select.Option>
              </Select>
            </Form.Item>

            <Form.Item
              label={t("emailTemplates.subject")}
              name="subject"
              rules={[{ required: true, message: t("emailTemplates.subjectRequired") }]}
            >
              <Input placeholder={t("emailTemplates.subjectPlaceholder")} />
            </Form.Item>

            <Form.Item
              label={t("emailTemplates.htmlBody")}
              name="htmlBody"
              rules={[{ required: true, message: t("emailTemplates.htmlBodyRequired") }]}
            >
              <TextArea
                rows={8}
                placeholder={t("emailTemplates.htmlBodyPlaceholder")}
              />
            </Form.Item>

            <Form.Item
              label={t("emailTemplates.plainTextBody")}
              name="textBody"
            >
              <TextArea
                rows={6}
                placeholder={t("emailTemplates.plainTextBodyPlaceholder")}
              />
            </Form.Item>

            <Form.Item label={t("emailTemplates.isDefault")} name="isDefault" valuePropName="checked">
              <Switch />
            </Form.Item>
          </Form>
        </Modal>
      </Card>
    </div>
  );
}
