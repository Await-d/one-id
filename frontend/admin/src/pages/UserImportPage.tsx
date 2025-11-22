import React, { useState, useEffect } from "react";
import { useTranslation } from "react-i18next";
import {
  Card,
  Button,
  Upload,
  Select,
  Alert,
  Table,
  Statistic,
  Row,
  Col,
  Descriptions,
  message,
  Space,
  Typography,
  Divider,
} from "antd";
import {
  UploadOutlined,
  DownloadOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  InfoCircleOutlined,
} from "@ant-design/icons";
import type { UploadFile } from "antd";
import { userImportApi, UserImportResult, ImportInstructions } from "../lib/userImportApi";
import { rolesApi, Role } from "../lib/rolesApi";

const { Title, Text, Paragraph } = Typography;

export default function UserImportPage() {
  const { t } = useTranslation();
  const [fileList, setFileList] = useState<UploadFile[]>([]);
  const [uploading, setUploading] = useState(false);
  const [importResult, setImportResult] = useState<UserImportResult | null>(null);
  const [instructions, setInstructions] = useState<ImportInstructions | null>(null);
  const [roles, setRoles] = useState<Role[]>([]);
  const [selectedRole, setSelectedRole] = useState<string | undefined>(undefined);
  const [downloading, setDownloading] = useState(false);

  useEffect(() => {
    loadInstructions();
    loadRoles();
  }, []);

  const loadInstructions = async () => {
    try {
      const data = await userImportApi.getInstructions();
      setInstructions(data);
      setSelectedRole(data.defaultRole);
    } catch (error) {
      console.error("Failed to load instructions", error);
    }
  };

  const loadRoles = async () => {
    try {
      const data = await rolesApi.getAll();
      setRoles(data);
    } catch (error) {
      console.error("Failed to load roles", error);
    }
  };

  const handleUpload = async () => {
    if (fileList.length === 0) {
      message.error(t('userImport.selectFileFirst'));
      return;
    }

    const file = fileList[0].originFileObj;
    if (!file) {
      message.error(t('userImport.invalidFile'));
      return;
    }

    setUploading(true);
    setImportResult(null);

    try {
      const result = await userImportApi.uploadCsv(file, selectedRole);
      setImportResult(result);

      if (result.successCount > 0) {
        message.success(t('userImport.importSuccess', { count: result.successCount }));
      }

      if (result.failureCount > 0) {
        message.warning(t('userImport.importWarning', { count: result.failureCount }));
      }

      // 清空文件列表
      setFileList([]);
    } catch (error: any) {
      message.error(error.message || t('userImport.importFailed'));
    } finally {
      setUploading(false);
    }
  };

  const handleDownloadSample = async () => {
    setDownloading(true);
    try {
      const blob = await userImportApi.downloadSample();
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = "user-import-sample.csv";
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
      message.success(t('userImport.downloadSampleSuccess'));
    } catch (error) {
      message.error(t('userImport.downloadSampleFailed'));
    } finally {
      setDownloading(false);
    }
  };

  const errorColumns = [
    {
      title: t('userImport.row'),
      dataIndex: "rowNumber",
      key: "rowNumber",
      width: 80,
    },
    {
      title: t('userImport.username'),
      dataIndex: "userName",
      key: "userName",
    },
    {
      title: t('userImport.email'),
      dataIndex: "email",
      key: "email",
    },
    {
      title: t('userImport.error'),
      dataIndex: "errorMessage",
      key: "errorMessage",
    },
  ];

  return (
    <div>
      <Title level={2}>📥 {t('userImport.title')}</Title>
      <Paragraph type="secondary">
        {t('userImport.subtitle')}
      </Paragraph>

      <Row gutter={[16, 16]}>
        {/* Instructions Card */}
        <Col xs={24} lg={12}>
          <Card
            title={
              <Space>
                <InfoCircleOutlined />
                <span>{t('userImport.instructions')}</span>
              </Space>
            }
            style={{ height: "100%" }}
          >
            {instructions && (
              <Descriptions column={1} size="small">
                <Descriptions.Item label={t('userImport.requiredColumns')}>
                  <Text code>{instructions.requiredColumns.join(", ")}</Text>
                </Descriptions.Item>
                <Descriptions.Item label={t('userImport.optionalColumns')}>
                  <Text code>{instructions.optionalColumns.join(", ")}</Text>
                </Descriptions.Item>
                <Descriptions.Item label={t('userImport.passwordRequirements')}>
                  {instructions.passwordRequirements}
                </Descriptions.Item>
                <Descriptions.Item label={t('userImport.maxFileSize')}>
                  {instructions.maxFileSize}
                </Descriptions.Item>
              </Descriptions>
            )}

            {instructions && instructions.notes.length > 0 && (
              <>
                <Divider />
                <Title level={5}>{t('userImport.importantNotes')}</Title>
                <ul style={{ paddingLeft: 20 }}>
                  {instructions.notes.map((note, index) => (
                    <li key={index}>
                      <Text>{note}</Text>
                    </li>
                  ))}
                </ul>
              </>
            )}

            <Divider />
            <Button
              type="link"
              icon={<DownloadOutlined />}
              onClick={handleDownloadSample}
              loading={downloading}
            >
              {t('userImport.downloadSample')}
            </Button>
          </Card>
        </Col>

        {/* Upload Card */}
        <Col xs={24} lg={12}>
          <Card title={t('userImport.uploadFile')}>
            <Space direction="vertical" style={{ width: "100%" }} size="large">
              <div>
                <Text strong>{t('userImport.defaultRole')}:</Text>
                <Select
                  style={{ width: "100%", marginTop: 8 }}
                  value={selectedRole}
                  onChange={setSelectedRole}
                  placeholder={t('userImport.selectRole')}
                >
                  {roles.map((role) => (
                    <Select.Option key={role.id} value={role.name}>
                      {role.name}
                    </Select.Option>
                  ))}
                </Select>
                <Text type="secondary" style={{ fontSize: 12 }}>
                  {t('userImport.defaultRoleHelp')}
                </Text>
              </div>

              <div>
                <Upload
                  fileList={fileList}
                  beforeUpload={(file) => {
                    if (!file.name.endsWith(".csv")) {
                      message.error(t('userImport.onlyCsvAllowed'));
                      return false;
                    }
                    setFileList([file as any]);
                    return false;
                  }}
                  onRemove={() => {
                    setFileList([]);
                  }}
                  accept=".csv"
                  maxCount={1}
                >
                  <Button icon={<UploadOutlined />}>{t('userImport.selectFile')}</Button>
                </Upload>
              </div>

              <Button
                type="primary"
                onClick={handleUpload}
                disabled={fileList.length === 0}
                loading={uploading}
                block
                size="large"
              >
                {uploading ? t('userImport.importing') : t('userImport.startImport')}
              </Button>
            </Space>
          </Card>
        </Col>
      </Row>

      {/* Import Results */}
      {importResult && (
        <Card title={t('userImport.importResults')} style={{ marginTop: 16 }}>
          <Row gutter={16}>
            <Col xs={24} sm={8}>
              <Statistic
                title={t('userImport.totalRows')}
                value={importResult.totalRows}
                prefix={<InfoCircleOutlined />}
              />
            </Col>
            <Col xs={24} sm={8}>
              <Statistic
                title={t('userImport.success')}
                value={importResult.successCount}
                valueStyle={{ color: "#3f8600" }}
                prefix={<CheckCircleOutlined />}
              />
            </Col>
            <Col xs={24} sm={8}>
              <Statistic
                title={t('userImport.failures')}
                value={importResult.failureCount}
                valueStyle={{ color: "#cf1322" }}
                prefix={<CloseCircleOutlined />}
              />
            </Col>
          </Row>

          {importResult.errors.length > 0 && (
            <>
              <Divider />
              <Alert
                message={t('userImport.importErrors')}
                description={t('userImport.importErrorsDesc', { count: importResult.errors.length })}
                type="error"
                showIcon
                style={{ marginBottom: 16 }}
              />
              <Table
                dataSource={importResult.errors}
                columns={errorColumns}
                rowKey={(record) => `${record.rowNumber}-${record.userName}`}
                pagination={{ pageSize: 10 }}
                size="small"
              />
            </>
          )}

          {importResult.successCount > 0 && importResult.failureCount === 0 && (
            <Alert
              message={t('userImport.allSuccess')}
              description={t('userImport.allSuccessDesc', { count: importResult.successCount })}
              type="success"
              showIcon
              style={{ marginTop: 16 }}
            />
          )}
        </Card>
      )}
    </div>
  );
}

