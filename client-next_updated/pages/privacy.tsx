import { useEffect, useState } from 'react';
import Layout from '../components/Layout';
import { contentApi } from '../lib/contentApi';
import { getApiBaseUrl } from '../lib/runtimeConfig';

function LegalDocument({ page }: { page: any }) {
  const documentUrl = page?.documentUrl?.startsWith('http') ? page.documentUrl : page?.documentUrl ? `${getApiBaseUrl()}${page.documentUrl}` : '';
  const isPdf = page?.documentContentType?.includes('pdf') || documentUrl.toLowerCase().endsWith('.pdf');

  if (!documentUrl) {
    return null;
  }

  return (
    <section className="surface-card p-5">
      <h1 className="section-title">{page.title || 'Privacy Policy'}</h1>
      <p className="section-copy mt-2">This policy is published as a managed legal document.</p>
      {isPdf ? (
        <iframe title="Privacy Policy document" src={documentUrl} className="mt-5 h-[72vh] w-full rounded-[24px] border border-[var(--client-border)] bg-white" />
      ) : (
        <div className="mt-5 rounded-[24px] border border-[var(--client-border)] bg-[var(--client-panel-soft)] p-5">
          <strong>{page.documentFileName || 'Privacy policy document'}</strong>
          <p className="section-copy mt-2">Word documents open best in a dedicated viewer or download.</p>
          <a href={documentUrl} target="_blank" rel="noreferrer" className="primary-action mt-4">Open document</a>
        </div>
      )}
    </section>
  );
}

export default function PrivacyPage() {
  const [page, setPage] = useState<any>(null);

  useEffect(() => {
    contentApi.getPage('privacy').then(setPage).catch(() => setPage(null));
  }, []);

  return (
    <Layout>
      {page?.sourceType !== 'Html' && page?.documentUrl ? <LegalDocument page={page} /> : (
        <article>
          <style>{page?.cssContent || ''}</style>
          <div dangerouslySetInnerHTML={{ __html: page?.htmlContent || '<h1>Privacy Policy</h1><p>Privacy policy content is being prepared.</p>' }} />
        </article>
      )}
    </Layout>
  );
}
