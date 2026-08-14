import { useEffect, useState } from 'react';
import Link from 'next/link';
import AdminShell from '../components/AdminShell';
import { adminApi } from '../lib/api';

const blankSlide = {
  isVisible: true,
  title: '',
  label: '',
  testimonial: '',
  author: '',
  imageUrl: '',
  imageAlt: '',
  displayOrder: 0,
};

const blankFeature = {
  title: '',
  body: '',
  isVisible: true,
  displayOrder: 0,
};

const defaultLanding = {
  isHeroMediaVisible: true,
  brandName: 'medicSocial',
  brandTagline: 'Healthcare hiring',
  badgeText: 'Open roles available now',
  headline: 'The specialized home for healthcare careers.',
  highlightText: 'healthcare',
  subheading: 'Connecting medical professionals, healthcare facilities, and hiring teams through verified profiles, configurable workflows, and a marketplace built for care work.',
  primaryCallToActionText: 'Find roles',
  primaryCallToActionUrl: '/jobs',
  secondaryCallToActionText: 'Join network',
  secondaryCallToActionUrl: '/register',
  heroSlides: [],
  featureCards: [],
  employerCalloutTitle: 'Hiring for a medical facility? Find vetted talent with a clearer pipeline.',
  employerCalloutBody: 'Post openings, configure requirements, manage applicants, invite matching professionals, and keep communication inside the same workspace.',
  journeySectionTitle: 'One platform. Two clear paths.',
  journeySectionBody: 'Create a free account to connect with the people and opportunities that move healthcare forward.',
  professionalJourneyTitle: 'For healthcare professionals',
  professionalJourneyBody: 'Build one trusted profile, discover suitable roles, and connect directly with potential employers.',
  employerJourneyTitle: 'For employers',
  employerJourneyBody: 'Grow a searchable talent pool, publish opportunities, and reach healthcare professionals ready for their next role.',
  freeAccessTitle: 'Start free in three simple steps',
  freeAccessBody: 'Choose your account type, create your free account, then complete your profile and start connecting.',
  isPublished: true,
};

export default function LandingEditorPage() {
  const [user, setUser] = useState(null);
  const [form, setForm] = useState(defaultLanding);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [previewSlide, setPreviewSlide] = useState(0);

  useEffect(() => {
    Promise.all([adminApi.getCurrentUser(), adminApi.getLandingPage()])
      .then(([currentUser, landing]) => {
        setUser(currentUser);
        setForm({
          ...defaultLanding,
          ...landing,
          heroSlides: Array.isArray(landing?.heroSlides) ? landing.heroSlides : [],
          featureCards: Array.isArray(landing?.featureCards) ? landing.featureCards : [],
        });
      })
      .catch((requestError) => setError(requestError.message || 'Unable to load landing page content.'));
  }, []);

  const updateField = (key, value) => setForm((current) => ({ ...current, [key]: value }));

  const updateSlide = (index, key, value) => {
    setForm((current) => ({
      ...current,
      heroSlides: current.heroSlides.map((item, currentIndex) => currentIndex === index ? { ...item, [key]: value } : item),
    }));
  };

  const updateFeature = (index, key, value) => {
    setForm((current) => ({
      ...current,
      featureCards: current.featureCards.map((item, currentIndex) => currentIndex === index ? { ...item, [key]: value } : item),
    }));
  };

  const addSlide = () => {
    setForm((current) => ({
      ...current,
      heroSlides: [...current.heroSlides, { ...blankSlide, displayOrder: current.heroSlides.length }],
    }));
  };

  const addFeature = () => {
    setForm((current) => ({
      ...current,
      featureCards: [...current.featureCards, { ...blankFeature, displayOrder: current.featureCards.length }],
    }));
  };

  const removeSlide = (index) => {
    setForm((current) => ({
      ...current,
      heroSlides: current.heroSlides.filter((_, currentIndex) => currentIndex !== index),
    }));
  };

  const removeFeature = (index) => {
    setForm((current) => ({
      ...current,
      featureCards: current.featureCards.filter((_, currentIndex) => currentIndex !== index),
    }));
  };

  const save = async (event) => {
    event.preventDefault();
    setError('');
    setMessage('');

    try {
      const saved = await adminApi.saveLandingPage({
        ...form,
        heroSlides: form.heroSlides.map((item, index) => ({ ...item, displayOrder: Number(item.displayOrder ?? index) })),
        featureCards: form.featureCards.map((item, index) => ({ ...item, displayOrder: Number(item.displayOrder ?? index) })),
      });
      setForm({ ...defaultLanding, ...saved });
      setMessage('Landing page content saved.');
      setTimeout(() => setMessage(''), 3200);
    } catch (requestError) {
      setError(requestError.message || 'Unable to save landing page content.');
      setTimeout(() => setError(''), 4500);
    }
  };

  const activeSlide = form.heroSlides[previewSlide] || form.heroSlides[0] || blankSlide;

  return (
    <AdminShell user={user} title="Landing Page Editor" subtitle="Control public landing page copy, image cards, testimonials, CTAs, and section visibility without redeploying the client UI.">
      {(message || error) && <div className="toast-stack"><div className={`toast ${error ? 'error' : 'success'}`}>{error || message}</div></div>}
      <Link href="/settings?tab=content" className="btn-secondary back-link">Back to content settings</Link>

      <form onSubmit={save} className="landing-editor">
        <section className="admin-card">
          <div className="section-heading">
            <div>
              <p className="eyebrow">Brand lockup</p>
              <h2>Header name and tagline</h2>
              <p>These values appear beside the logo mark on the public client navigation and footer.</p>
            </div>
          </div>
          <div className="form-grid spacious-form">
            <label className="field-label">Brand name<input className="input" value={form.brandName || ''} onChange={(event) => updateField('brandName', event.target.value)} /></label>
            <label className="field-label">Tagline<input className="input" value={form.brandTagline || ''} onChange={(event) => updateField('brandTagline', event.target.value)} /></label>
          </div>
        </section>

        <section className="admin-card">
          <div className="section-heading">
            <div>
              <p className="eyebrow">Audience journeys</p>
              <h2>Why visitors should join</h2>
              <p>Keep this section brief. It appears immediately below the public hero.</p>
            </div>
          </div>

          <div className="form-grid spacious-form">
            <label className="field-label full-span">Section title<input className="input" value={form.journeySectionTitle || ''} onChange={(event) => updateField('journeySectionTitle', event.target.value)} /></label>
            <label className="field-label full-span">Section introduction<textarea className="input textarea" value={form.journeySectionBody || ''} onChange={(event) => updateField('journeySectionBody', event.target.value)} /></label>
            <label className="field-label">Professional journey title<input className="input" value={form.professionalJourneyTitle || ''} onChange={(event) => updateField('professionalJourneyTitle', event.target.value)} /></label>
            <label className="field-label">Employer journey title<input className="input" value={form.employerJourneyTitle || ''} onChange={(event) => updateField('employerJourneyTitle', event.target.value)} /></label>
            <label className="field-label">Professional benefits<textarea className="input textarea" value={form.professionalJourneyBody || ''} onChange={(event) => updateField('professionalJourneyBody', event.target.value)} /></label>
            <label className="field-label">Employer benefits<textarea className="input textarea" value={form.employerJourneyBody || ''} onChange={(event) => updateField('employerJourneyBody', event.target.value)} /></label>
            <label className="field-label">Free access title<input className="input" value={form.freeAccessTitle || ''} onChange={(event) => updateField('freeAccessTitle', event.target.value)} /></label>
            <label className="field-label">Free access guidance<textarea className="input textarea" value={form.freeAccessBody || ''} onChange={(event) => updateField('freeAccessBody', event.target.value)} /></label>
          </div>
        </section>

        <section className="admin-card">
          <div className="section-heading">
            <div>
              <p className="eyebrow">Public hero</p>
              <h2>Headline and calls to action</h2>
              <p>These values drive the first section visitors see on the client landing page.</p>
            </div>
            <label className="switch-card compact"><input type="checkbox" checked={form.isPublished} onChange={(event) => updateField('isPublished', event.target.checked)} /> Published</label>
          </div>

          <div className="form-grid spacious-form">
            <label className="field-label">Badge text<input className="input" value={form.badgeText} onChange={(event) => updateField('badgeText', event.target.value)} /></label>
            <label className="field-label">Highlight word<input className="input" value={form.highlightText} onChange={(event) => updateField('highlightText', event.target.value)} /></label>
            <label className="field-label full-span">Headline<input className="input" value={form.headline} onChange={(event) => updateField('headline', event.target.value)} /></label>
            <label className="field-label full-span">Subheading<textarea className="input textarea" value={form.subheading} onChange={(event) => updateField('subheading', event.target.value)} /></label>
            <label className="field-label">Primary CTA text<input className="input" value={form.primaryCallToActionText} onChange={(event) => updateField('primaryCallToActionText', event.target.value)} /></label>
            <label className="field-label">Primary CTA URL<input className="input" value={form.primaryCallToActionUrl} onChange={(event) => updateField('primaryCallToActionUrl', event.target.value)} /></label>
            <label className="field-label">Secondary CTA text<input className="input" value={form.secondaryCallToActionText} onChange={(event) => updateField('secondaryCallToActionText', event.target.value)} /></label>
            <label className="field-label">Secondary CTA URL<input className="input" value={form.secondaryCallToActionUrl} onChange={(event) => updateField('secondaryCallToActionUrl', event.target.value)} /></label>
          </div>
        </section>

        <section className="admin-card">
          <div className="section-heading">
            <div>
              <p className="eyebrow">Hero media and testimonials</p>
              <h2>Editable image/story cards</h2>
              <p>Use stock image URLs, hosted campaign images, or any publicly reachable image URL.</p>
            </div>
            <div className="button-row">
              <label className="switch-card compact"><input type="checkbox" checked={form.isHeroMediaVisible} onChange={(event) => updateField('isHeroMediaVisible', event.target.checked)} /> Show section</label>
              <button type="button" className="btn-secondary" onClick={addSlide}>Add slide</button>
            </div>
          </div>

          <div className="landing-editor-grid">
            <div className="config-list">
              {form.heroSlides.map((slide, index) => (
                <button key={`${slide.title}-${index}`} type="button" className={`config-list-card interactive ${previewSlide === index ? 'selected' : ''}`} onClick={() => setPreviewSlide(index)}>
                  <strong>{slide.title || `Slide ${index + 1}`}</strong>
                  <span>{slide.isVisible ? 'Visible' : 'Hidden'} - {slide.label || 'No label set'}</span>
                </button>
              ))}
              {form.heroSlides.length === 0 && <p className="empty-state">No slides configured yet.</p>}
            </div>

            {form.heroSlides.map((slide, index) => previewSlide === index && (
              <div key={`editor-${index}`} className="nested-editor">
                <div className="preview-frame">
                  {slide.imageUrl ? <img src={slide.imageUrl} alt={slide.imageAlt || slide.title || 'Landing slide preview'} /> : <div className="image-placeholder">No image URL</div>}
                  <div>
                    <strong>{slide.title || 'Untitled slide'}</strong>
                    <span>{slide.testimonial || 'No testimonial text yet.'}</span>
                  </div>
                </div>
                <div className="form-grid spacious-form">
                  <label className="field-label">Title<input className="input" value={slide.title} onChange={(event) => updateSlide(index, 'title', event.target.value)} /></label>
                  <label className="field-label">Label<input className="input" value={slide.label} onChange={(event) => updateSlide(index, 'label', event.target.value)} /></label>
                  <label className="field-label full-span">Image URL<input className="input" value={slide.imageUrl} onChange={(event) => updateSlide(index, 'imageUrl', event.target.value)} /></label>
                  <label className="field-label full-span">Image alt text<input className="input" value={slide.imageAlt} onChange={(event) => updateSlide(index, 'imageAlt', event.target.value)} /></label>
                  <label className="field-label full-span">Testimonial<textarea className="input textarea" value={slide.testimonial} onChange={(event) => updateSlide(index, 'testimonial', event.target.value)} /></label>
                  <label className="field-label">Author/source<input className="input" value={slide.author} onChange={(event) => updateSlide(index, 'author', event.target.value)} /></label>
                  <label className="field-label">Display order<input className="input" type="number" value={slide.displayOrder} onChange={(event) => updateSlide(index, 'displayOrder', Number(event.target.value))} /></label>
                  <label className="switch-card compact"><input type="checkbox" checked={slide.isVisible} onChange={(event) => updateSlide(index, 'isVisible', event.target.checked)} /> Visible</label>
                </div>
                <div className="button-row">
                  <button type="button" className="btn-danger" onClick={() => removeSlide(index)}>Remove slide</button>
                </div>
              </div>
            ))}
          </div>
        </section>

        <section className="admin-card">
          <div className="section-heading">
            <div>
              <p className="eyebrow">Feature and employer sections</p>
              <h2>Supporting landing content</h2>
              <p>These cards preserve the platform capability messaging below the hero.</p>
            </div>
            <button type="button" className="btn-secondary" onClick={addFeature}>Add feature</button>
          </div>

          <div className="form-grid spacious-form">
            <label className="field-label full-span">Employer callout title<input className="input" value={form.employerCalloutTitle} onChange={(event) => updateField('employerCalloutTitle', event.target.value)} /></label>
            <label className="field-label full-span">Employer callout body<textarea className="input textarea" value={form.employerCalloutBody} onChange={(event) => updateField('employerCalloutBody', event.target.value)} /></label>
          </div>

          <div className="config-list config-list-wide" style={{ marginTop: 18 }}>
            {form.featureCards.map((feature, index) => (
              <div key={`feature-${index}`} className="config-list-card">
                <div className="form-grid spacious-form">
                  <label className="field-label">Feature title<input className="input" value={feature.title} onChange={(event) => updateFeature(index, 'title', event.target.value)} /></label>
                  <label className="field-label">Display order<input className="input" type="number" value={feature.displayOrder} onChange={(event) => updateFeature(index, 'displayOrder', Number(event.target.value))} /></label>
                  <label className="field-label full-span">Body<textarea className="input textarea" value={feature.body} onChange={(event) => updateFeature(index, 'body', event.target.value)} /></label>
                  <label className="switch-card compact"><input type="checkbox" checked={feature.isVisible} onChange={(event) => updateFeature(index, 'isVisible', event.target.checked)} /> Visible</label>
                </div>
                <div className="button-row">
                  <button type="button" className="btn-danger" onClick={() => removeFeature(index)}>Remove feature</button>
                </div>
              </div>
            ))}
          </div>
        </section>

        <div className="button-row sticky-actions">
          <button type="submit" className="btn-primary">Save landing page</button>
        </div>
      </form>

      <style jsx>{`
        .landing-editor {
          display: grid;
          gap: 22px;
          margin-top: 22px;
        }

        .admin-card {
          border: 1px solid rgba(148, 163, 184, 0.28);
          border-radius: 28px;
          background: rgba(255, 255, 255, 0.92);
          padding: 24px;
          box-shadow: 0 20px 55px rgba(15, 23, 42, 0.08);
        }

        .section-heading {
          display: flex;
          align-items: flex-start;
          justify-content: space-between;
          gap: 20px;
          border-bottom: 1px solid rgba(148, 163, 184, 0.2);
          margin-bottom: 20px;
          padding-bottom: 18px;
        }

        .section-heading h2 {
          margin: 4px 0;
          color: #07122b;
          font-size: 28px;
          font-weight: 900;
          letter-spacing: -0.04em;
        }

        .section-heading p {
          margin: 0;
          color: #64748b;
        }

        .eyebrow {
          color: #5e8078 !important;
          font-size: 12px;
          font-weight: 900;
          letter-spacing: 0.2em;
          text-transform: uppercase;
        }

        .full-span {
          grid-column: 1 / -1;
        }

        .textarea {
          min-height: 110px;
          padding-top: 14px;
        }

        .landing-editor-grid {
          display: grid;
          grid-template-columns: minmax(220px, 0.58fr) minmax(0, 1.42fr);
          gap: 20px;
          align-items: start;
        }

        .nested-editor {
          display: grid;
          gap: 18px;
        }

        .preview-frame {
          display: grid;
          grid-template-columns: 220px 1fr;
          gap: 18px;
          align-items: center;
          border: 1px solid rgba(148, 163, 184, 0.28);
          border-radius: 24px;
          background: #f8fafc;
          padding: 14px;
        }

        .preview-frame img,
        .image-placeholder {
          width: 100%;
          height: 150px;
          border-radius: 18px;
          object-fit: cover;
          background: #e2e8f0;
        }

        .image-placeholder {
          display: grid;
          place-items: center;
          color: #64748b;
          font-weight: 800;
        }

        .preview-frame strong,
        .preview-frame span {
          display: block;
        }

        .preview-frame strong {
          color: #07122b;
          font-size: 24px;
          font-weight: 900;
        }

        .preview-frame span {
          margin-top: 8px;
          color: #64748b;
          line-height: 1.6;
        }

        .config-list-card.selected {
          border-color: #5e8078;
          background: #eef6f2;
        }

        .sticky-actions {
          position: sticky;
          bottom: 18px;
          justify-content: flex-end;
          border: 1px solid rgba(148, 163, 184, 0.28);
          border-radius: 24px;
          background: rgba(255, 255, 255, 0.9);
          padding: 14px;
          backdrop-filter: blur(12px);
        }

        @media (max-width: 900px) {
          .section-heading,
          .landing-editor-grid,
          .preview-frame {
            grid-template-columns: 1fr;
          }

          .section-heading {
            display: grid;
          }
        }
      `}</style>
    </AdminShell>
  );
}
