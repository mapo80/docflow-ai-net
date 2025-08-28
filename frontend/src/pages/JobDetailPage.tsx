import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Alert, Row, Col, Switch } from 'antd';
import { JobsService, OpenAPI } from '../generated';
import { parseOutputToViewModel, type ExtractionViewModel } from '../adapters/extractionAdapter';
import DocumentPreview from '../components/DocumentPreview';
import FieldsTable from '../components/FieldsTable';
import Loader from '../components/Loader';
import { useApiError } from '../components/ApiErrorProvider';

export default function JobDetailPage() {
  const { id } = useParams();
  const [model, setModel] = useState<ExtractionViewModel | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const { showError } = useApiError();

  const [selectedFieldId, setSelectedFieldId] = useState<string>();
  const [selectedWordIds, setSelectedWordIds] = useState<Set<string>>(new Set());
  const [currentPage, setCurrentPage] = useState(1);
  const [zoom, setZoom] = useState(1);
  const [showOnlySelected, setShowOnlySelected] = useState(false);

  useEffect(() => {
    const load = async () => {
      if (!id) return;
      setLoading(true);
      try {
        const job = await JobsService.jobsGetById({ id });
        if (!job.paths?.output?.path) {
          setError('No output available');
          setModel(null);
          return;
        }
        let outUrl = job.paths.output.path;
        if (!outUrl.startsWith('http')) outUrl = `${OpenAPI.BASE}${outUrl}`;
        let outputJson: any;
        for (let i = 0; i < 3; i++) {
          const res = await fetch(outUrl, {
            headers: OpenAPI.HEADERS as Record<string, string> | undefined,
          });
          if (res.status === 429 && res.headers.get('Retry-After')) {
            const waitMs = parseInt(res.headers.get('Retry-After')!, 10) * 1000;
            await new Promise((r) => setTimeout(r, waitMs));
            continue;
          }
          if (!res.ok) throw new Error('Failed to fetch output');
          outputJson = await res.json();
          break;
        }
        const vm = parseOutputToViewModel(job, outputJson);
        if (!vm || vm.pages.length === 0) {
          setError('No bounding boxes available');
          setModel(null);
          return;
        }
        setModel(vm);
        setCurrentPage(vm.pages[0].index);
        setError(null);
      } catch (e: any) {
        setError(e.message);
        showError(e.message);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [id, showError]);

  const handleFieldSelect = (fieldId: string) => {
    if (!model) return;
    setSelectedFieldId(fieldId);
    const field = model.fields.find((f) => f.id === fieldId);
    const wordSet = new Set(field?.wordIds || []);
    setSelectedWordIds(wordSet);
    if (field?.page && field.page !== currentPage) {
      setCurrentPage(field.page);
    }
  };

  const handleWordClick = (wordId: string) => {
    if (!model) return;
    const field = model.fields.find((f) => f.wordIds.includes(wordId));
    if (field) {
      handleFieldSelect(field.id);
    } else {
      setSelectedFieldId(undefined);
      setSelectedWordIds(new Set([wordId]));
    }
  };

  if (loading) return <Loader />;
  if (error) return <Alert type="error" message={error} />;
  if (!model) return <Alert message="No data" type="warning" />;

  return (
    <Row gutter={16}>
      <Col span={16}>
        <DocumentPreview
          docType={model.docType}
          srcUrl={model.srcUrl}
          pages={model.pages}
          currentPage={currentPage}
          zoom={zoom}
          showOnlySelected={showOnlySelected}
          selectedWordIds={selectedWordIds}
          onWordClick={handleWordClick}
          onPageChange={setCurrentPage}
          onZoomChange={setZoom}
          onToggleShowOnly={setShowOnlySelected}
        />
      </Col>
      <Col span={8}>
        <FieldsTable
          fields={model.fields}
          selectedFieldId={selectedFieldId}
          onFieldSelect={handleFieldSelect}
        />
        <Switch
          checked={showOnlySelected}
          onChange={setShowOnlySelected}
          checkedChildren="Only selected"
          unCheckedChildren="All"
          style={{ marginTop: 8 }}
        />
      </Col>
    </Row>
  );
}
