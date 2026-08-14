import { useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import AdminShell from '../components/AdminShell';
import { adminApi } from '../lib/api';
import { buildApiUrl, buildClientUrl } from '../lib/runtimeConfig';

const defaultCss = `.legal-page-shell {
  --ink: #141412;
  --muted: #66645d;
  --paper: #fffdf8;
  --moss: #51624f;
  --line: rgba(20, 20, 18, 0.12);
  color: var(--ink);
  font-family: Inter, Segoe UI, sans-serif;
}
.legal-hero {
  border-radius: 34px;
  padding: clamp(2rem, 5vw, 5rem);
  background: linear-gradient(135deg, #181715 0%, #4d2c24 48%, #1e332f 100%);
  color: #fffdf8;
}
.legal-hero h1 {
  max-width: 860px;
  margin: 1rem 0;
  font-size: clamp(3rem, 8vw, 7rem);
  line-height: .9;
  letter-spacing: -.07em;
}
.legal-grid {
  display: grid;
  grid-template-columns: minmax(0, .78fr) minmax(0, 1.22fr);
  gap: 1.5rem;
  margin-top: 2rem;
}
.legal-nav, .legal-content-card {
  border: 1px solid var(--line);
  border-radius: 30px;
  background: rgba(255,253,248,.88);
  padding: 1.25rem;
}
.legal-nav a {
  display: block;
  border-radius: 18px;
  padding: .75rem 1rem;
  color: var(--ink);
  font-weight: 800;
  text-decoration: none;
}
.legal-content-card section {
  border-top: 1px solid var(--line);
  padding-top: 1.5rem;
  margin-top: 1.5rem;
}
.legal-content-card section:first-child {
  border-top: 0;
  padding-top: 0;
  margin-top: 0;
}
.legal-content-card p, .legal-content-card li {
  color: var(--muted);
  line-height: 1.85;
}
@media (max-width: 860px) {
  .legal-grid { grid-template-columns: 1fr; }
}`;

const defaultPages = {
  privacy: {
    slug: 'privacy',
    title: 'Privacy Policy',
    htmlContent: '<article class="legal-page-shell"><header class="legal-hero"><h1>Privacy Policy</h1><p>Explain how the platform collects, uses, protects, and shares user, employer, professional, verification, payment, social, messaging, and audit information.</p></header><div class="legal-grid"><aside class="legal-nav"><a href="#data">Information we collect</a><a href="#use">How we use it</a><a href="#rights">Your choices</a></aside><div class="legal-content-card"><section id="data"><h2>Information we collect</h2><p>Account, profile, document, application, communication, payment, device, session, and audit records needed to run the platform.</p></section><section id="use"><h2>How we use it</h2><p>To operate hiring workflows, verify information, manage subscriptions, deliver notifications, moderate feed activity, and secure accounts.</p></section><section id="rights"><h2>Your choices</h2><p>Users can update account information and contact platform administrators for privacy or verification support.</p></section></div></div></article>',
    cssContent: defaultCss,
    sourceType: 'Html',
    documentUrl: '',
    documentFileName: '',
    documentContentType: '',
    documentSizeBytes: null,
    isPublished: true,
  },
  terms: {
    slug: 'terms',
    title: 'Terms and Conditions',
    htmlContent: `<article class="legal-page-shell"><header class="legal-hero"><h1>Terms and Conditions</h1><p>These terms govern use of MedicSocial by healthcare professionals and employers and explain each user's responsibility when evaluating an opportunity or connection.</p></header><div class="legal-grid"><aside class="legal-nav"><a href="#accounts">Accounts</a><a href="#responsibilities">User responsibilities</a><a href="#due-diligence">Due diligence and fraud risk</a><a href="#platform-role">Platform role</a><a href="#liability">Liability</a><a href="#indemnity">Indemnity</a><a href="#moderation">Moderation</a></aside><div class="legal-content-card"><section id="accounts"><h2>Accounts</h2><p>Users must provide accurate information and keep their identity, profile, facility, professional, and verification records complete and current.</p></section><section id="responsibilities"><h2>User responsibilities</h2><p>Before making or accepting an offer, employers must independently verify a professional's identity, current licences, qualifications, references, authority to work, and suitability. Professionals must independently verify an employer's identity, legal existence where applicable, authority to hire, facility, workplace conditions, role, written offer, contract terms, and the authority of each person with whom they deal.</p></section><section id="due-diligence"><h2>User due diligence, fraud risk and platform disclaimer</h2><p>Every user must exercise independent judgment and careful due diligence before relying on another user, disclosing sensitive information, transferring money, attending a workplace, signing a contract, or providing services. Users must remain alert to fraud, impersonation, falsified credentials, misleading information, payment scams, unsafe workplaces, misconduct, and contractual disputes. A profile, document, badge, status, result, message, introduction, or other platform feature is not a warranty that any person, credential, facility, opportunity, representation, or transaction is genuine, accurate, lawful, safe, suitable, or current. Users must verify all material information through independent and authoritative sources.</p></section><section id="platform-role"><h2>Platform role and off-platform dealings</h2><p>MedicSocial provides technology for discovering opportunities and making initial connections and acts only as an intermediary. It is not an employer, recruitment or employment agency, contracting party, guarantor, regulator, credentialing body, background-check provider, or professional-verification authority. Subsequent conversations, checks, negotiations, offers, contracts, payments, work, and related dealings are expected to occur independently and may occur off the platform. MedicSocial does not supervise, control, monitor, or routinely follow up on those dealings and does not determine whether an employment or locum engagement was concluded, safe, satisfactory, or successful. Any agreement is solely between the relevant employer and professional.</p></section><section id="liability"><h2>Disclaimer and limitation of liability</h2><p>To the fullest extent permitted by applicable law, MedicSocial and its present or future operators, administrators, developers, representatives, service providers, successors, and affiliates provide the platform and user-supplied content on an “as is” and “as available” basis without warranties concerning identity, credentials, accuracy, authenticity, conduct, safety, suitability, outcomes, or payment. They are not responsible for user or third-party acts, omissions, offers, workplaces, services, payments, or disputes, including fraud, impersonation, credential falsification, scams, unsafe conditions, misconduct, non-payment, breach of contract, or unsuccessful employment or locum proceedings. To the fullest extent permitted by law, they are not liable for losses connected with user-to-user or off-platform dealings, reliance on user-supplied information, or inadequate due diligence. Nothing excludes or limits liability that applicable law does not permit to be excluded or limited.</p></section><section id="indemnity"><h2>Indemnity</h2><p>To the fullest extent permitted by applicable law, each user agrees to defend, indemnify, and hold harmless MedicSocial and its present or future operators, administrators, developers, representatives, service providers, successors, and affiliates from third-party claims, proceedings, liabilities, losses, damages, penalties, and reasonable costs arising from that user's content, representations, conduct, off-platform dealings, employment or locum arrangements, breach of these terms, infringement of another person's rights, or violation of law, except to the extent applicable law does not permit such indemnification.</p></section><section id="moderation"><h2>Moderation</h2><p>MedicSocial may restrict content or access and take proportionate action where platform rules, trust, safety, or legal compliance require intervention. This discretionary moderation does not create a duty to monitor users or off-platform dealings.</p></section></div></div></article>`,
    cssContent: defaultCss,
    sourceType: 'Html',
    documentUrl: '',
    documentFileName: '',
    documentContentType: '',
    documentSizeBytes: null,
    isPublished: true,
  },
};

function mergePage(slug, pages) {
  const existing = pages.find((page) => page.slug === slug);
  return { ...defaultPages[slug], ...(existing || {}) };
}

function decodeHtml(value) {
  return String(value || '')
    .replace(/&amp;/g, '&')
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&quot;/g, '"')
    .replace(/&#39;/g, "'");
}

function escapeHtml(value) {
  return String(value || '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

function sanitizeHref(value) {
  const href = String(value || '').trim();
  if (!href) {
    return '#';
  }

  if (href.startsWith('#') || href.startsWith('/') || href.startsWith('./') || href.startsWith('../')) {
    return href;
  }

  try {
    const parsed = new URL(href);
    if (['http:', 'https:', 'mailto:', 'tel:'].includes(parsed.protocol)) {
      return href;
    }
  } catch {
    return '#';
  }

  return '#';
}

function parseLegalHtml(htmlContent, fallbackTitle) {
  const fallback = {
    heroTitle: fallbackTitle || 'Legal page',
    heroBody: '',
    navLinks: [],
    sections: [],
  };

  if (!htmlContent) {
    return fallback;
  }

  if (typeof window !== 'undefined' && window.DOMParser) {
    const documentNode = new window.DOMParser().parseFromString(htmlContent, 'text/html');
    const hero = documentNode.querySelector('.legal-hero');
    const navLinks = Array.from(documentNode.querySelectorAll('.legal-nav a')).map((link) => ({
      href: link.getAttribute('href') || '',
      label: link.textContent || '',
    }));
    const sections = Array.from(documentNode.querySelectorAll('.legal-content-card section')).map((section, index) => ({
      id: section.getAttribute('id') || `section-${index + 1}`,
      title: section.querySelector('h2')?.textContent || `Section ${index + 1}`,
      body: section.querySelector('p')?.textContent || '',
    }));

    return {
      heroTitle: hero?.querySelector('h1')?.textContent || fallback.heroTitle,
      heroBody: hero?.querySelector('p')?.textContent || fallback.heroBody,
      navLinks,
      sections,
    };
  }

  const sectionMatches = [...htmlContent.matchAll(/<section[^>]*id="([^"]*)"[^>]*>\s*<h2>(.*?)<\/h2>\s*<p>(.*?)<\/p>\s*<\/section>/gis)];
  return {
    heroTitle: decodeHtml(htmlContent.match(/<h1>(.*?)<\/h1>/is)?.[1] || fallback.heroTitle),
    heroBody: decodeHtml(htmlContent.match(/<header[^>]*class="legal-hero"[^>]*>[\s\S]*?<p>(.*?)<\/p>/is)?.[1] || fallback.heroBody),
    navLinks: [...htmlContent.matchAll(/<a\s+href="([^"]*)">(.*?)<\/a>/gis)].map((match) => ({ href: match[1], label: decodeHtml(match[2]) })),
    sections: sectionMatches.map((match, index) => ({ id: match[1] || `section-${index + 1}`, title: decodeHtml(match[2]), body: decodeHtml(match[3]) })),
  };
}

function buildLegalHtml(model) {
  const sections = model.sections.length ? model.sections : [{ id: 'summary', title: 'Summary', body: 'Add the first page section.' }];
  const navLinks = model.navLinks.length ? model.navLinks : sections.map((section) => ({ href: `#${section.id}`, label: section.title }));
  return `<article class="legal-page-shell"><header class="legal-hero"><h1>${escapeHtml(model.heroTitle)}</h1><p>${escapeHtml(model.heroBody)}</p></header><div class="legal-grid"><aside class="legal-nav">${navLinks.map((link) => `<a href="${escapeHtml(sanitizeHref(link.href || '#'))}">${escapeHtml(link.label)}</a>`).join('')}</aside><div class="legal-content-card">${sections.map((section) => `<section id="${escapeHtml(section.id)}"><h2>${escapeHtml(section.title)}</h2><p>${escapeHtml(section.body)}</p></section>`).join('')}</div></div></article>`;
}

export default function LegalPagesEditor() {
  const [user, setUser] = useState(null);
  const [selectedSlug, setSelectedSlug] = useState('privacy');
  const [pages, setPages] = useState(defaultPages);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [uploading, setUploading] = useState(false);
  const [focusedSection, setFocusedSection] = useState('hero');

  useEffect(() => {
    Promise.all([adminApi.getCurrentUser(), adminApi.getContentPages()])
      .then(([currentUser, contentPages]) => {
        setUser(currentUser);
        const nextPages = {
          privacy: mergePage('privacy', Array.isArray(contentPages) ? contentPages : []),
          terms: mergePage('terms', Array.isArray(contentPages) ? contentPages : []),
        };
        setPages(nextPages);
      })
      .catch((requestError) => setError(requestError.message || 'Unable to load legal pages.'));
  }, []);

  const selectedPage = pages[selectedSlug];
  const structuredPage = useMemo(() => parseLegalHtml(selectedPage.htmlContent, selectedPage.title), [selectedPage.htmlContent, selectedPage.title]);

  const updateSelected = (key, value) => {
    setPages((current) => ({
      ...current,
      [selectedSlug]: { ...current[selectedSlug], [key]: value },
    }));
  };

  const updateStructuredPage = (updater) => {
    const nextModel = updater(structuredPage);
    updateSelected('htmlContent', buildLegalHtml(nextModel));
  };

  const save = async (publish) => {
    setError('');
    setMessage('');
    try {
      const saved = await adminApi.saveContentPage({ ...selectedPage, isPublished: publish });
      setPages((current) => ({ ...current, [selectedSlug]: saved }));
      setMessage(publish ? 'Legal page published.' : 'Legal page saved as draft.');
      setTimeout(() => setMessage(''), 3000);
    } catch (requestError) {
      setError(requestError.message || 'Unable to save legal page.');
      setTimeout(() => setError(''), 4500);
    }
  };

  const publicPath = useMemo(() => selectedSlug === 'privacy' ? '/privacy' : '/terms', [selectedSlug]);
  const publicUrl = useMemo(() => buildClientUrl(publicPath), [publicPath]);
  const documentUrl = selectedPage.documentUrl?.startsWith('http') ? selectedPage.documentUrl : selectedPage.documentUrl ? buildApiUrl(selectedPage.documentUrl) : '';
  const isDocumentPage = selectedPage.sourceType === 'UploadedDocument' || selectedPage.sourceType === 'ExternalDocument';
  const isPdf = (selectedPage.documentContentType || '').includes('pdf') || documentUrl.toLowerCase().endsWith('.pdf');

  const uploadDocument = async (event) => {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    setError('');
    setMessage('');
    setUploading(true);
    try {
      const saved = await adminApi.uploadContentPageDocument(selectedSlug, file);
      setPages((current) => ({ ...current, [selectedSlug]: { ...current[selectedSlug], ...saved } }));
      setMessage('Legal document uploaded.');
      setTimeout(() => setMessage(''), 3000);
    } catch (requestError) {
      setError(requestError.message || 'Unable to upload legal document.');
      setTimeout(() => setError(''), 4500);
    } finally {
      setUploading(false);
      event.target.value = '';
    }
  };

  return (
    <AdminShell user={user} title="Legal Pages" subtitle="Edit, preview, save drafts, and publish the public privacy and terms pages linked from the client footer.">
      {(message || error) && <div className="toast-stack"><div className={`toast ${error ? 'error' : 'success'}`}>{error || message}</div></div>}
      <Link href="/settings?tab=content" className="btn-secondary back-link">Back to content settings</Link>

      <div className="legal-editor-shell">
        <section className="admin-card editor-panel">
          <div className="tab-row">
            <button type="button" className={selectedSlug === 'privacy' ? 'active' : ''} onClick={() => setSelectedSlug('privacy')}>Privacy Policy</button>
            <button type="button" className={selectedSlug === 'terms' ? 'active' : ''} onClick={() => setSelectedSlug('terms')}>Terms and Conditions</button>
          </div>

          <div className="form-grid spacious-form">
            <label className="field-label">Public URL<input className="input" value={publicPath} readOnly /></label>
            <label className="field-label">Page title<input className="input" value={selectedPage.title} onChange={(event) => updateSelected('title', event.target.value)} /></label>
            <label className="field-label">Publication source
              <select className="input" value={selectedPage.sourceType || 'Html'} onChange={(event) => updateSelected('sourceType', event.target.value)}>
                <option value="Html">Designed HTML page</option>
                <option value="UploadedDocument">Uploaded PDF/Word document</option>
                <option value="ExternalDocument">External document link</option>
              </select>
            </label>
            <label className="field-label">External document URL<input className="input" value={selectedPage.documentUrl || ''} onChange={(event) => updateSelected('documentUrl', event.target.value)} placeholder="https://example.com/privacy.pdf" /></label>
            <label className="switch-card compact"><input type="checkbox" checked={selectedPage.isPublished} onChange={(event) => updateSelected('isPublished', event.target.checked)} /> Published</label>
            <label className="field-label full-span upload-zone">Upload PDF or Word document
              <input type="file" accept=".pdf,.doc,.docx,application/pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document" onChange={uploadDocument} />
              <span>{uploading ? 'Uploading...' : 'Supported: PDF, DOC, DOCX up to 10 MB.'}</span>
            </label>
            {selectedPage.documentFileName && (
              <div className="document-card full-span">
                <strong>{selectedPage.documentFileName}</strong>
                <span>{selectedPage.documentContentType || 'Document'} - {selectedPage.documentSizeBytes ? `${Math.round(selectedPage.documentSizeBytes / 1024)} KB` : 'Size unavailable'}</span>
              </div>
            )}
            <div className="structured-editor full-span">
              <div className="structured-editor-header">
                <div>
                  <p className="eyebrow">Section editor</p>
                  <h3>Edit page content by visible blocks</h3>
                </div>
                <button type="button" className="btn-secondary" onClick={() => {
                  const nextNumber = structuredPage.sections.length + 1;
                  updateStructuredPage((model) => ({
                    ...model,
                    sections: [...model.sections, { id: `section-${nextNumber}`, title: `New section ${nextNumber}`, body: 'Describe this policy section.' }],
                    navLinks: [...model.navLinks, { href: `#section-${nextNumber}`, label: `New section ${nextNumber}` }],
                  }));
                  setFocusedSection(`section-${nextNumber}`);
                }}>Add section</button>
              </div>

              <details className={`legal-edit-card ${focusedSection === 'hero' ? 'active' : ''}`} open={focusedSection === 'hero'}>
                <summary onClick={() => setFocusedSection('hero')}>Hero block</summary>
                <div className="legal-edit-card-body">
                  <label className="field-label">Hero title<input className="input" value={structuredPage.heroTitle} onChange={(event) => updateStructuredPage((model) => ({ ...model, heroTitle: event.target.value }))} /></label>
                  <label className="field-label">Hero intro<textarea className="input text-area-compact" value={structuredPage.heroBody} onChange={(event) => updateStructuredPage((model) => ({ ...model, heroBody: event.target.value }))} /></label>
                </div>
              </details>

              <details className={`legal-edit-card ${focusedSection === 'navigation' ? 'active' : ''}`} open={focusedSection === 'navigation'}>
                <summary onClick={() => setFocusedSection('navigation')}>Navigation links and hrefs</summary>
                <div className="legal-link-list">
                  {structuredPage.navLinks.map((link, index) => (
                    <div key={`${link.href}-${index}`} className="legal-link-row">
                      <label className="field-label">Visible link text<input className="input" value={link.label} onChange={(event) => updateStructuredPage((model) => ({ ...model, navLinks: model.navLinks.map((item, currentIndex) => currentIndex === index ? { ...item, label: event.target.value } : item) }))} /></label>
                      <label className="field-label">Safe href or anchor<input className="input" value={link.href} onChange={(event) => updateStructuredPage((model) => ({ ...model, navLinks: model.navLinks.map((item, currentIndex) => currentIndex === index ? { ...item, href: sanitizeHref(event.target.value) } : item) }))} /></label>
                    </div>
                  ))}
                </div>
              </details>

              {structuredPage.sections.map((section, index) => (
                <details key={`${section.id}-${index}`} className={`legal-edit-card ${focusedSection === section.id ? 'active' : ''}`} open={focusedSection === section.id}>
                  <summary onClick={() => setFocusedSection(section.id)}>{section.title || `Section ${index + 1}`}</summary>
                  <div className="legal-edit-card-body">
                    <label className="field-label">Anchor id<input className="input" value={section.id} onChange={(event) => updateStructuredPage((model) => ({ ...model, sections: model.sections.map((item, currentIndex) => currentIndex === index ? { ...item, id: event.target.value.replace(/[^a-zA-Z0-9-_]/g, '') } : item) }))} /></label>
                    <label className="field-label">Section heading<input className="input" value={section.title} onChange={(event) => updateStructuredPage((model) => ({ ...model, sections: model.sections.map((item, currentIndex) => currentIndex === index ? { ...item, title: event.target.value } : item) }))} /></label>
                    <label className="field-label full-span">Section body<textarea className="input text-area-compact" value={section.body} onChange={(event) => updateStructuredPage((model) => ({ ...model, sections: model.sections.map((item, currentIndex) => currentIndex === index ? { ...item, body: event.target.value } : item) }))} /></label>
                    <button type="button" className="btn-secondary" onClick={() => updateStructuredPage((model) => ({ ...model, sections: model.sections.filter((_, currentIndex) => currentIndex !== index), navLinks: model.navLinks.filter((link) => link.href !== `#${section.id}`) }))}>Remove section</button>
                  </div>
                </details>
              ))}
            </div>
            <details className="advanced-code full-span">
              <summary>Advanced HTML and CSS</summary>
              <label className="field-label full-span">HTML content<textarea className="input code-area" value={selectedPage.htmlContent} onChange={(event) => updateSelected('htmlContent', event.target.value)} /></label>
              <label className="field-label full-span">CSS styles<textarea className="input code-area" value={selectedPage.cssContent} onChange={(event) => updateSelected('cssContent', event.target.value)} /></label>
            </details>
          </div>

          <div className="button-row">
            <button type="button" className="btn-secondary" onClick={() => save(false)}>Save draft</button>
            <button type="button" className="btn-primary" onClick={() => save(true)}>Publish page</button>
          </div>
        </section>

        <section className="admin-card preview-panel">
          <div className="section-heading compact-heading">
            <div>
              <p className="eyebrow">Preview</p>
              <h2>{selectedPage.title}</h2>
            </div>
            <a className="btn-secondary" href={publicUrl} target="_blank" rel="noreferrer">Open public page</a>
          </div>
          {!isDocumentPage && (
            <div className="preview-jump-row">
              <button type="button" onClick={() => setFocusedSection('hero')}>Edit hero</button>
              <button type="button" onClick={() => setFocusedSection('navigation')}>Edit links</button>
              {structuredPage.sections.map((section) => (
                <button key={section.id} type="button" onClick={() => setFocusedSection(section.id)}>Edit {section.title}</button>
              ))}
            </div>
          )}
          <div className="legal-preview">
            {isDocumentPage && documentUrl ? (
              isPdf ? (
                <iframe title={`${selectedPage.title} preview`} src={documentUrl} />
              ) : (
                <div className="document-preview-card">
                  <strong>{selectedPage.documentFileName || selectedPage.title}</strong>
                  <span>Word documents cannot be embedded reliably in every browser. Open the document to review it.</span>
                  <a className="btn-primary" href={documentUrl} target="_blank" rel="noreferrer">Open document</a>
                </div>
              )
            ) : (
              <>
                <style>{selectedPage.cssContent}</style>
                <div dangerouslySetInnerHTML={{ __html: selectedPage.htmlContent }} />
              </>
            )}
          </div>
        </section>
      </div>

      <style jsx global>{`
        .legal-editor-shell {
          display: grid;
          grid-template-columns: minmax(320px, 0.8fr) minmax(0, 1.2fr);
          gap: 22px;
          margin-top: 22px;
          align-items: start;
        }

        .admin-card {
          border: 1px solid rgba(148, 163, 184, 0.28);
          border-radius: 28px;
          background: rgba(255, 255, 255, 0.92);
          padding: 24px;
          box-shadow: 0 20px 55px rgba(15, 23, 42, 0.08);
        }

        .tab-row {
          display: grid;
          grid-template-columns: repeat(2, minmax(0, 1fr));
          gap: 10px;
          margin-bottom: 18px;
        }

        .tab-row button {
          border: 1px solid rgba(148, 163, 184, 0.34);
          border-radius: 18px;
          background: #f8fafc;
          color: #334155;
          padding: 13px 15px;
          font-weight: 900;
          cursor: pointer;
        }

        .tab-row button.active {
          border-color: #5e8078;
          background: #eef6f2;
          color: #07122b;
        }

        .full-span {
          grid-column: 1 / -1;
        }

        .code-area {
          min-height: 210px;
          font-family: Consolas, Monaco, monospace;
          font-size: 13px;
          line-height: 1.55;
        }

        .structured-editor,
        .advanced-code {
          display: grid;
          gap: 12px;
          border: 1px solid rgba(148, 163, 184, 0.26);
          border-radius: 24px;
          background: linear-gradient(145deg, rgba(255,255,255,0.96), rgba(248,250,252,0.92));
          padding: 16px;
        }

        .structured-editor-header,
        .legal-edit-card summary {
          display: flex;
          align-items: center;
          justify-content: space-between;
          gap: 12px;
        }

        .structured-editor-header h3 {
          margin: 4px 0 0;
          color: #07122b;
          font-size: 22px;
          font-weight: 950;
          letter-spacing: -0.04em;
        }

        .legal-edit-card {
          border: 1px solid rgba(148, 163, 184, 0.28);
          border-radius: 20px;
          background: #fff;
          overflow: hidden;
        }

        .legal-edit-card.active {
          border-color: #5e8078;
          box-shadow: 0 14px 35px rgba(94, 128, 120, 0.12);
        }

        .legal-edit-card summary,
        .advanced-code summary {
          cursor: pointer;
          padding: 14px 16px;
          color: #07122b;
          font-weight: 950;
          list-style: none;
        }

        .legal-edit-card summary::-webkit-details-marker,
        .advanced-code summary::-webkit-details-marker {
          display: none;
        }

        .legal-edit-card-body,
        .legal-link-list {
          display: grid;
          grid-template-columns: repeat(2, minmax(0, 1fr));
          gap: 12px;
          border-top: 1px solid rgba(148, 163, 184, 0.18);
          padding: 16px;
        }

        .legal-link-row {
          display: grid;
          grid-template-columns: repeat(2, minmax(0, 1fr));
          gap: 12px;
          grid-column: 1 / -1;
        }

        .text-area-compact {
          min-height: 110px;
          padding: 12px;
        }

        .advanced-code[open] {
          gap: 14px;
        }

        .preview-panel {
          overflow: hidden;
        }

        .legal-preview {
          max-height: 72vh;
          min-height: 540px;
          overflow: auto;
          border: 1px solid rgba(148, 163, 184, 0.24);
          border-radius: 26px;
          background: #fffaf2;
          padding: 18px;
        }

        .preview-jump-row {
          display: flex;
          flex-wrap: wrap;
          gap: 8px;
          margin-bottom: 14px;
        }

        .preview-jump-row button {
          border: 1px solid rgba(94, 128, 120, 0.3);
          border-radius: 999px;
          background: #eef6f2;
          color: #34564f;
          padding: 8px 12px;
          font-size: 12px;
          font-weight: 900;
          cursor: pointer;
        }

        .legal-preview iframe {
          width: 100%;
          min-height: 68vh;
          border: 0;
          border-radius: 18px;
          background: #fff;
        }

        .upload-zone {
          border: 1px dashed rgba(94, 128, 120, 0.45);
          border-radius: 20px;
          background: #f8fafc;
          padding: 16px;
        }

        .upload-zone input {
          margin-top: 10px;
        }

        .upload-zone span,
        .document-card span,
        .document-preview-card span {
          display: block;
          color: #64748b;
          font-size: 13px;
          font-weight: 700;
          margin-top: 6px;
        }

        .document-card,
        .document-preview-card {
          border: 1px solid rgba(148, 163, 184, 0.28);
          border-radius: 20px;
          background: #f8fafc;
          padding: 16px;
        }

        .document-preview-card {
          display: grid;
          gap: 12px;
          place-items: start;
        }

        .compact-heading {
          display: flex;
          align-items: center;
          justify-content: space-between;
          gap: 16px;
          margin-bottom: 16px;
        }

        .compact-heading h2 {
          margin: 4px 0 0;
          font-size: 28px;
          letter-spacing: -0.04em;
        }

        .eyebrow {
          margin: 0;
          color: #5e8078;
          font-size: 12px;
          font-weight: 900;
          letter-spacing: 0.2em;
          text-transform: uppercase;
        }

        @media (max-width: 1080px) {
          .legal-editor-shell {
            grid-template-columns: 1fr;
          }
        }
      `}</style>
    </AdminShell>
  );
}
