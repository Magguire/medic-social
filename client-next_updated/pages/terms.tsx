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
      <h1 className="section-title">{page.title || 'Terms and Conditions'}</h1>
      <p className="section-copy mt-2">These terms are published as a managed legal document.</p>
      {isPdf ? (
        <iframe title="Terms and Conditions document" src={documentUrl} className="mt-5 h-[72vh] w-full rounded-[24px] border border-[var(--client-border)] bg-white" />
      ) : (
        <div className="mt-5 rounded-[24px] border border-[var(--client-border)] bg-[var(--client-panel-soft)] p-5">
          <strong>{page.documentFileName || 'Terms and conditions document'}</strong>
          <p className="section-copy mt-2">Word documents open best in a dedicated viewer or download.</p>
          <a href={documentUrl} target="_blank" rel="noreferrer" className="primary-action mt-4">Open document</a>
        </div>
      )}
    </section>
  );
}

export default function TermsPage() {
  const [page, setPage] = useState<any>(null);

  useEffect(() => {
    contentApi.getPage('terms').then(setPage).catch(() => setPage(null));
  }, []);

  return (
    <Layout>
      {page?.sourceType !== 'Html' && page?.documentUrl ? <LegalDocument page={page} /> : (
        <article>
          <style>{page?.cssContent || ''}</style>
          <div dangerouslySetInnerHTML={{ __html: page?.htmlContent || '<h1>Terms and Conditions</h1><p>Terms and conditions content is being prepared.</p>' }} />
        </article>
      )}
    </Layout>
  );
}
