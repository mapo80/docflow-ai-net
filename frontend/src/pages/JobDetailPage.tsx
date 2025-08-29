import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Alert, Row, Col, Grid } from 'antd';
import { JobsService, OpenAPI } from '../generated';
import {
  parseOutputToViewModel,
  type ExtractionViewModel,
} from '../adapters/extractionAdapter';
import DocumentPreview from '../components/DocumentPreview';
import FieldsTable from '../components/FieldsTable';
import Loader from '../components/Loader';
import { useApiError } from '../components/ApiErrorProvider';

interface Props {
  jobId?: string;
}

export default function JobDetailPage({ jobId }: Props) {
  const params = useParams();
  const id = jobId ?? params.id;
  const [model, setModel] = useState<ExtractionViewModel | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const { showError } = useApiError();

  const [selectedFieldId, setSelectedFieldId] = useState<string>();
  const [selectedWordIds, setSelectedWordIds] = useState<Set<string>>(new Set());
  const [currentPage, setCurrentPage] = useState(1);
  const [zoom, setZoom] = useState(1);
  const screens = Grid.useBreakpoint();
  const isMobile = !screens.md;
  const BBOX_ZOOM = 1.5;

  useEffect(() => {
    const load = async () => {
      if (!id) return;
      setLoading(true);
      try {
        const job = await JobsService.jobsGetById({ id });
        if (!job.paths?.output?.path || !job.paths?.markdown?.path) {
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
        let mdUrl = job.paths.markdownJson?.path || job.paths.markdown.path.replace(/\.md$/i, '.json');
        if (!mdUrl.startsWith('http')) mdUrl = `${OpenAPI.BASE}${mdUrl}`;
        const mdRes = await fetch(mdUrl, {
          headers: OpenAPI.HEADERS as Record<string, string> | undefined,
        });
        if (!mdRes.ok) throw new Error('Failed to fetch markdown');
        const mdJson = await mdRes.json();
        const vm = parseOutputToViewModel(job, outputJson, mdJson);
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
    if (selectedFieldId === fieldId) {
      setSelectedFieldId(undefined);
      setSelectedWordIds(new Set());
      return;
    }
    const field = model.fields.find((f) => f.id === fieldId);
    const wordSet = new Set(field?.wordIds || []);
    setSelectedFieldId(fieldId);
    setSelectedWordIds(wordSet);
    if (field?.page && field.page !== currentPage) {
      setCurrentPage(field.page);
    }
    if (wordSet.size > 0) setZoom(BBOX_ZOOM);
  };

  const handleWordClick = (wordId: string) => {
    if (!model) return;
    const field = model.fields.find((f) => f.wordIds.includes(wordId));
    if (field) {
      if (selectedFieldId !== field.id) {
        handleFieldSelect(field.id);
      }
    } else {
      const already =
        selectedWordIds.has(wordId) &&
        selectedWordIds.size === 1 &&
        !selectedFieldId;
      setSelectedFieldId(undefined);
      setSelectedWordIds(already ? new Set() : new Set([wordId]));
      if (!already) setZoom(BBOX_ZOOM);
    }
  };

  if (loading) return <Loader />;
  if (error) return <Alert type="error" message={error} />;
  if (!model) return <Alert message="No data" type="warning" />;

  if (isMobile) {
    return (
      <div style={{ display: 'flex', flexDirection: 'column', height: 'calc(100vh - 64px)' }}>
        <div style={{ flex: 3, minHeight: 0 }}>
          <DocumentPreview
            docType={model.docType}
            srcUrl={model.srcUrl}
            pages={model.pages}
            currentPage={currentPage}
            zoom={zoom}
            selectedWordIds={selectedWordIds}
            onWordClick={handleWordClick}
            onPageChange={setCurrentPage}
            onZoomChange={setZoom}
            fitWidth={false}
          />
        </div>
        <div style={{ flex: 2, minHeight: 0, overflow: 'auto' }}>
          <FieldsTable
            docType={model.docType}
            fields={model.fields}
            selectedFieldId={selectedFieldId}
            onFieldSelect={handleFieldSelect}
            isMobile
          />
        </div>
      </div>
    );
  }

  return (
    <Row gutter={[16, 16]} style={{ height: 'calc(100vh - 64px)' }}>
      <Col span={14} style={{ height: '100%' }}>
        <DocumentPreview
          docType={model.docType}
          srcUrl={model.srcUrl}
          pages={model.pages}
          currentPage={currentPage}
          zoom={zoom}
          selectedWordIds={selectedWordIds}
          onWordClick={handleWordClick}
          onPageChange={setCurrentPage}
          onZoomChange={setZoom}
          fitWidth
        />
      </Col>
      <Col span={10} style={{ height: '100%', overflow: 'auto' }}>
        <FieldsTable
          docType={model.docType}
          fields={model.fields}
          selectedFieldId={selectedFieldId}
          onFieldSelect={handleFieldSelect}
        />
      </Col>
    </Row>
  );
}
