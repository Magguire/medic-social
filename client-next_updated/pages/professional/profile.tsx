import { useEffect, useMemo, useState } from 'react';
import Layout from '../../components/Layout';
import CountryAutocomplete from '../../components/CountryAutocomplete';
import LanguageMultiSelect from '../../components/LanguageMultiSelect';
import PhoneInput from '../../components/PhoneInput';
import { authApi } from '../../lib/authApi';
import { professionalApi } from '../../lib/professionalApi';
import { verificationApi } from '../../lib/verificationApi';
import { useAuth, useRequireAuth } from '../../lib/useAuth';
import type {
  EducationRecord,
  ExperienceRecord,
  ProfessionalCategory,
  ProfessionalDocument,
  ProfessionalProfile,
  QualificationRecord,
  RequiredDocumentRuleSummary,
} from '../../types';

type ConfiguredDocumentType = {
  name: string;
  slug: string;
  targetType: string;
  description?: string;
  allowedExtensions?: string;
  maxFileSizeMb?: number;
};

const availabilityOptions = ['FullTime', 'PartTime', 'Contract', 'Locum'];
const employmentTypes = ['Full time', 'Part time', 'Contract', 'Internship', 'Volunteer', 'Locum'];
const preferredLocationOptions = ['On-site', 'Remote', 'Hybrid', 'Open to relocation', 'Flexible'];
const fieldLabelClass = 'mb-1.5 block text-sm font-semibold text-slate-700';
const parseAllowedExtensions = (value?: string) =>
  (value || '')
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean);

const splitStoredCategories = (value?: string | null) =>
  (value || '')
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean);

export default function ProfessionalProfilePage() {
  const { hydrated } = useRequireAuth();
  const { user } = useAuth();
  const [categories, setCategories] = useState<ProfessionalCategory[]>([]);
  const [documentTypes, setDocumentTypes] = useState<ConfiguredDocumentType[]>([]);
  const [requiredDocuments, setRequiredDocuments] = useState<RequiredDocumentRuleSummary[]>([]);
  const [profile, setProfile] = useState<ProfessionalProfile | null>(null);
  const [education, setEducation] = useState<EducationRecord[]>([]);
  const [qualifications, setQualifications] = useState<QualificationRecord[]>([]);
  const [experiences, setExperiences] = useState<ExperienceRecord[]>([]);
  const [documents, setDocuments] = useState<ProfessionalDocument[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [activeStep, setActiveStep] = useState<'profile' | 'education' | 'experience' | 'skills' | 'documents' | 'security'>('profile');
  const [selectedCategoryIds, setSelectedCategoryIds] = useState<string[]>([]);
  const [otherCategoryValue, setOtherCategoryValue] = useState('');
  const [registrationForm, setRegistrationForm] = useState({
    nationality: '',
    phoneNumber: '',
    emailAddress: '',
    nationalIdOrPassport: '',
    addressLine: '',
    city: '',
    county: '',
    postalAddress: '',
    licenseNumber: '',
    licenseBoard: '',
    yearsOfExperience: 0,
    specialty: '',
  });
  const [updateForm, setUpdateForm] = useState({
    nationality: '',
    phoneNumber: '',
    emailAddress: '',
    nationalIdOrPassport: '',
    addressLine: '',
    city: '',
    county: '',
    postalAddress: '',
    professionalCategory: '',
    specialty: '',
    bio: '',
    licenseNumber: '',
    licenseBoard: '',
    licenseExpiryDate: '',
    yearsOfExperience: 0,
    currentPosition: '',
    currentEmployer: '',
    preferredLocation: '',
    relocationWillingness: 0,
    expectedSalary: '',
    availabilityType: 'FullTime',
    skills: '',
    languages: '',
    workPermitStatus: '',
  });
  const [educationForm, setEducationForm] = useState({ institution: '', award: '', fieldOfStudy: '', startDate: '', endDate: '', grade: '' });
  const [qualificationForm, setQualificationForm] = useState({ title: '', issuingBody: '', licenseNumber: '', issuedOn: '', expiresOn: '' });
  const [experienceForm, setExperienceForm] = useState({ employerName: '', jobTitle: '', employmentType: 'Full time', location: '', startDate: '', endDate: '', isCurrentRole: false, responsibilities: '' });
  const [documentType, setDocumentType] = useState('');
  const [documentFile, setDocumentFile] = useState<File | null>(null);
  const [passwordForm, setPasswordForm] = useState({ currentPassword: '', newPassword: '', confirmNewPassword: '' });
  const [passwordMessage, setPasswordMessage] = useState<string | null>(null);

  const selectedDocumentType = useMemo(
    () => documentTypes.find((item) => item.slug === documentType || item.name === documentType) || null,
    [documentType, documentTypes],
  );

  const requiredDocumentStatus = useMemo(() => requiredDocuments.map((rule) => ({
    ...rule,
    uploaded: documents.some((document) => document.type === rule.documentType),
  })), [requiredDocuments, documents]);

  const syncSelectedCategories = (storedValue: string | null | undefined, categoryList: ProfessionalCategory[]) => {
    const savedCategories = splitStoredCategories(storedValue);
    const matchedIds = categoryList
      .filter((category) => savedCategories.includes(category.id) || savedCategories.includes(category.slug) || savedCategories.includes(category.name))
      .map((category) => category.id);
    setSelectedCategoryIds(matchedIds);
    const otherValue = savedCategories.find((item) => item.toLowerCase().startsWith('other:'));
    setOtherCategoryValue(otherValue ? otherValue.split(':').slice(1).join(':').trim() : '');
  };

  const loadRequiredDocuments = async (categoryList: ProfessionalCategory[], selectedIds: string[], fallbackStoredCategories?: string | null) => {
    const matchedCategories = categoryList
      .filter((category) => selectedIds.includes(category.id))
      .map((category) => category.name);
    const fallbackCategories = splitStoredCategories(fallbackStoredCategories).filter((item) => !item.toLowerCase().startsWith('other:'));
    const categoryNames = Array.from(new Set([...(matchedCategories || []), ...fallbackCategories]));
    if (categoryNames.length === 0) {
      setRequiredDocuments(await verificationApi.getRequiredDocuments('Professional').catch(() => []));
      return;
    }

    const ruleSets = await Promise.all(categoryNames.map((categoryName) => verificationApi.getRequiredDocuments('Professional', { category: categoryName }).catch(() => [])));
    const merged = new Map<string, RequiredDocumentRuleSummary>();
    ruleSets.flat().forEach((item) => {
      const existing = merged.get(item.documentType);
      if (!existing || item.isMandatory) {
        merged.set(item.documentType, item);
      }
    });
    setRequiredDocuments(Array.from(merged.values()));
  };

  const refreshProfile = async (userId: string, categoryList: ProfessionalCategory[] = categories) => {
    const currentProfile = await professionalApi.getProfileByUser(userId);
    setProfile(currentProfile);
    setUpdateForm({
      nationality: currentProfile.nationality || '',
      phoneNumber: currentProfile.phoneNumber || '',
      emailAddress: currentProfile.emailAddress || user?.email || '',
      nationalIdOrPassport: currentProfile.nationalIdOrPassport || '',
      addressLine: currentProfile.addressLine || '',
      city: currentProfile.city || '',
      county: currentProfile.county || '',
      postalAddress: currentProfile.postalAddress || '',
      professionalCategory: currentProfile.professionalCategory || '',
      specialty: currentProfile.specialty || '',
      bio: currentProfile.bio || '',
      licenseNumber: currentProfile.licenseNumber || '',
      licenseBoard: currentProfile.licenseBoard || '',
      licenseExpiryDate: currentProfile.licenseExpiryDate ? currentProfile.licenseExpiryDate.slice(0, 10) : '',
      yearsOfExperience: currentProfile.yearsOfExperience,
      currentPosition: currentProfile.currentPosition || '',
      currentEmployer: currentProfile.currentEmployer || '',
      preferredLocation: currentProfile.preferredLocation || '',
      relocationWillingness: currentProfile.relocationWillingness || 0,
      expectedSalary: currentProfile.expectedSalary ? String(currentProfile.expectedSalary) : '',
      availabilityType: currentProfile.availabilityType || 'FullTime',
      skills: currentProfile.skills || '',
      languages: currentProfile.languages || '',
      workPermitStatus: currentProfile.workPermitStatus || '',
    });
    syncSelectedCategories(currentProfile.professionalCategory, categoryList);
    const [edu, quals, exp, docs] = await Promise.all([
      professionalApi.getEducation(currentProfile.id),
      professionalApi.getQualifications(currentProfile.id),
      professionalApi.getExperience(currentProfile.id),
      professionalApi.getDocuments(currentProfile.id),
    ]);
    setEducation(edu);
    setQualifications(quals);
    setExperiences(exp);
    setDocuments(docs);
    await loadRequiredDocuments(
      categoryList,
      categoryList
        .filter((category) => splitStoredCategories(currentProfile.professionalCategory).includes(category.name) || splitStoredCategories(currentProfile.professionalCategory).includes(category.slug) || splitStoredCategories(currentProfile.professionalCategory).includes(category.id))
        .map((category) => category.id),
      currentProfile.professionalCategory,
    );
    return currentProfile;
  };

  useEffect(() => {
    if (!hydrated || !user) return;

    const load = async () => {
      const [loadedCategories, loadedDocumentTypes] = await Promise.all([
        professionalApi.getCategories().catch(() => []),
        verificationApi.getDocumentTypes('Professional').catch(() => []),
      ]);
      setCategories(loadedCategories);
      setDocumentTypes(loadedDocumentTypes);
      setDocumentType(loadedDocumentTypes[0]?.slug || '');
      setRegistrationForm((current) => ({ ...current, emailAddress: user.email || current.emailAddress }));

      try {
        await refreshProfile(user.id, loadedCategories);
      } catch {
        setProfile(null);
        setDocuments([]);
        setEducation([]);
        setQualifications([]);
        setExperiences([]);
        await loadRequiredDocuments(loadedCategories, []);
      }
    };

    load().catch(() => undefined);
  }, [hydrated, user]);

  const buildCategoryPayload = () => {
    const values = categories
      .filter((category) => selectedCategoryIds.includes(category.id))
      .map((category) => category.id);

    const hasOther = categories.some((category) => selectedCategoryIds.includes(category.id) && category.name.toLowerCase() === 'other');
    if (hasOther) {
      if (!otherCategoryValue.trim()) {
        throw new Error('Enter the other professional category.');
      }

      values.push(`Other: ${otherCategoryValue.trim()}`);
    }

    return values.join(', ');
  };

  const toggleCategory = async (id: string) => {
    const nextIds = selectedCategoryIds.includes(id)
      ? selectedCategoryIds.filter((item) => item !== id)
      : [...selectedCategoryIds, id];
    setSelectedCategoryIds(nextIds);
    await loadRequiredDocuments(categories, nextIds, profile?.professionalCategory);
  };

  const createProfile = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!user) return;
    try {
      setSaving(true);
      const professionalCategory = buildCategoryPayload();
      await professionalApi.register({
        userId: user.id,
        nationality: registrationForm.nationality,
        phoneNumber: registrationForm.phoneNumber,
        emailAddress: registrationForm.emailAddress || user.email,
        nationalIdOrPassport: registrationForm.nationalIdOrPassport,
        addressLine: registrationForm.addressLine,
        city: registrationForm.city,
        county: registrationForm.county,
        postalAddress: registrationForm.postalAddress,
        professionalCategory,
        licenseNumber: registrationForm.licenseNumber,
        licenseBoard: registrationForm.licenseBoard,
        yearsOfExperience: registrationForm.yearsOfExperience,
        specialty: registrationForm.specialty,
      });
      await refreshProfile(user.id, categories);
      setError(null);
    } catch (requestError: any) {
      setError(requestError.response?.data?.errors?.[0] || requestError.message || 'Unable to create professional profile.');
    } finally {
      setSaving(false);
    }
  };

  const updateProfile = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!profile) return;
    try {
      setSaving(true);
      const professionalCategory = buildCategoryPayload();
      await professionalApi.updateProfile(profile.id, {
        nationality: updateForm.nationality,
        phoneNumber: updateForm.phoneNumber,
        emailAddress: updateForm.emailAddress,
        nationalIdOrPassport: updateForm.nationalIdOrPassport,
        addressLine: updateForm.addressLine,
        city: updateForm.city,
        county: updateForm.county,
        postalAddress: updateForm.postalAddress,
        bio: updateForm.bio,
        yearsOfExperience: updateForm.yearsOfExperience,
        currentPosition: updateForm.currentPosition,
        currentEmployer: updateForm.currentEmployer,
        preferredLocation: updateForm.preferredLocation,
        relocationWillingness: updateForm.relocationWillingness,
        expectedSalary: Number(updateForm.expectedSalary || 0),
        availabilityType: updateForm.availabilityType,
        professionalCategory,
        licenseExpiryDate: updateForm.licenseExpiryDate || undefined,
        skills: updateForm.skills,
        languages: updateForm.languages,
        workPermitStatus: updateForm.workPermitStatus,
        specialty: updateForm.specialty,
      });
      await refreshProfile(user!.id, categories);
      setError(null);
    } catch (requestError: any) {
      setError(requestError.response?.data?.errors?.[0] || 'Unable to update profile.');
    } finally {
      setSaving(false);
    }
  };

  const addEducation = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!profile) return;
    const created = await professionalApi.addEducation(profile.id, educationForm);
    setEducation((current) => [created, ...current]);
    setEducationForm({ institution: '', award: '', fieldOfStudy: '', startDate: '', endDate: '', grade: '' });
  };

  const addQualification = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!profile) return;
    const created = await professionalApi.addQualification(profile.id, qualificationForm);
    setQualifications((current) => [created, ...current]);
    setQualificationForm({ title: '', issuingBody: '', licenseNumber: '', issuedOn: '', expiresOn: '' });
  };

  const addExperience = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!profile) return;
    const created = await professionalApi.addExperience(profile.id, experienceForm);
    setExperiences((current) => [created, ...current]);
    setExperienceForm({ employerName: '', jobTitle: '', employmentType: 'Full time', location: '', startDate: '', endDate: '', isCurrentRole: false, responsibilities: '' });
  };

  const uploadDocument = async () => {
    if (!profile || !documentFile || !documentType) return;
    try {
      setSaving(true);
      await professionalApi.uploadDocument(profile.id, documentType, documentFile);
      setDocuments(await professionalApi.getDocuments(profile.id));
      setDocumentFile(null);
      setError(null);
    } catch (requestError: any) {
      setError(requestError.response?.data?.errors?.[0] || 'Unable to upload document.');
    } finally {
      setSaving(false);
    }
  };

  const updatePassword = async (event: React.FormEvent) => {
    event.preventDefault();
    try {
      setSaving(true);
      await authApi.changePassword(passwordForm.currentPassword, passwordForm.newPassword, passwordForm.confirmNewPassword);
      setPasswordMessage('Password updated successfully.');
      setPasswordForm({ currentPassword: '', newPassword: '', confirmNewPassword: '' });
      setError(null);
    } catch (requestError: any) {
      setPasswordMessage(null);
      setError(requestError.response?.data?.errors?.[0] || 'Unable to update password.');
    } finally {
      setSaving(false);
    }
  };

  const renderRequiredDocuments = () => (
    <div className="rounded-3xl bg-slate-50 px-4 py-4 text-sm text-slate-600">
      <p className="font-semibold text-slate-900">Required profile documents</p>
      <div className="mt-3 grid gap-3 md:grid-cols-2">
        {requiredDocumentStatus.length === 0 && <p className="text-slate-500">No required documents are currently listed for your selected category.</p>}
        {requiredDocumentStatus.map((rule) => (
          <div key={rule.id} className="rounded-2xl border border-slate-200 bg-white px-4 py-3">
            <div className="flex items-center justify-between gap-3">
              <span className="font-semibold text-slate-900">{rule.documentType}</span>
              <span className={`pill-chip ${rule.uploaded ? 'bg-emerald-100 text-emerald-700' : ''}`}>{rule.uploaded ? 'Uploaded' : rule.isMandatory ? 'Required' : 'Optional'}</span>
            </div>
            {rule.appliesTo && <p className="mt-2 text-xs text-slate-500">Applies to: {rule.appliesTo}</p>}
          </div>
        ))}
      </div>
    </div>
  );

  return (
    <Layout>
      <section className="space-y-6">
        <div className="surface-card overflow-hidden">
          <div className="bg-[linear-gradient(120deg,#0f2b63,#0f67c7)] px-6 py-7 text-white">
            <p className="text-sm font-semibold uppercase tracking-[0.32em] text-blue-100">Professional onboarding</p>
            <h1 className="mt-3 text-3xl font-black tracking-tight">Build a complete healthcare profile with realistic career and verification details.</h1>
            <p className="mt-3 max-w-3xl text-blue-50">Keep your profile, education, work experience, skills, and documents ready for applications.</p>
          </div>
        </div>

        {error && <div className="rounded-2xl bg-rose-50 px-4 py-3 text-sm font-semibold text-rose-700">{error}</div>}

        <div className="client-stepper">
          {[
            ['profile', 'Profile'],
            ['education', 'Education'],
            ['experience', 'Experience'],
            ['skills', 'Skills'],
            ['documents', 'Documents'],
            ['security', 'Security'],
          ].map(([key, label]) => {
            const requiresProfile = key !== 'profile';
            const disabled = requiresProfile && !profile;
            return (
              <button
                key={key}
                type="button"
                className={activeStep === key ? 'active' : ''}
                disabled={disabled}
                onClick={() => {
                  if (!disabled) setActiveStep(key as typeof activeStep);
                }}
              >
                {label}
              </button>
            );
          })}
        </div>

        {!profile && activeStep === 'profile' ? (
          <form className="surface-card p-6" onSubmit={createProfile}>
            <h2 className="section-title">Create your professional profile</h2>
            <p className="section-copy mt-2">Start with identity, contact, licensing, and category information so the right verification and document rules can load.</p>

            <details className="mt-5 rounded-3xl border border-slate-200 bg-white px-4 py-4" open>
              <summary className="cursor-pointer text-lg font-semibold text-slate-900">Identity and contact</summary>
              <div className="profile-form-grid mt-4">
                <div>
                  <CountryAutocomplete label="Nationality" value={registrationForm.nationality} onChange={(value) => setRegistrationForm((current) => ({ ...current, nationality: value }))} />
                </div>
                <label>
                  <span className={fieldLabelClass}>Email address</span>
                  <input className="input-shell" value={registrationForm.emailAddress} onChange={(event) => setRegistrationForm((current) => ({ ...current, emailAddress: event.target.value }))} placeholder="name@example.com" />
                </label>
                <div>
                  <PhoneInput label="Phone number" countryValue={registrationForm.nationality} phoneValue={registrationForm.phoneNumber} onCountryChange={(value) => setRegistrationForm((current) => ({ ...current, nationality: value }))} onPhoneChange={(value) => setRegistrationForm((current) => ({ ...current, phoneNumber: value }))} />
                </div>
                <label>
                  <span className={fieldLabelClass}>National ID or passport</span>
                  <input className="input-shell" value={registrationForm.nationalIdOrPassport} onChange={(event) => setRegistrationForm((current) => ({ ...current, nationalIdOrPassport: event.target.value }))} placeholder="Government ID or passport number" />
                </label>
                <label className="md:col-span-2">
                  <span className={fieldLabelClass}>Address line</span>
                  <input className="input-shell" value={registrationForm.addressLine} onChange={(event) => setRegistrationForm((current) => ({ ...current, addressLine: event.target.value }))} placeholder="Street, building, suite, or district" />
                </label>
                <label>
                  <span className={fieldLabelClass}>City or town</span>
                  <input className="input-shell" value={registrationForm.city} onChange={(event) => setRegistrationForm((current) => ({ ...current, city: event.target.value }))} placeholder="City or town" />
                </label>
                <label>
                  <span className={fieldLabelClass}>State, province, or region</span>
                  <input className="input-shell" value={registrationForm.county} onChange={(event) => setRegistrationForm((current) => ({ ...current, county: event.target.value }))} placeholder="State, province, or region" />
                </label>
                <label>
                  <span className={fieldLabelClass}>Postal address</span>
                  <input className="input-shell" value={registrationForm.postalAddress} onChange={(event) => setRegistrationForm((current) => ({ ...current, postalAddress: event.target.value }))} placeholder="Postal or mailing address" />
                </label>
              </div>
            </details>

            <details className="mt-4 rounded-3xl border border-slate-200 bg-white px-4 py-4" open>
              <summary className="cursor-pointer text-lg font-semibold text-slate-900">Professional registration</summary>
              <div className="profile-form-grid mt-4">
                <div className="md:col-span-2">
                  <span className={fieldLabelClass}>Professional categories</span>
                  <div className="client-choice-grid">
                    {categories.map((category) => (
                      <button type="button" key={category.id} className={`client-choice ${selectedCategoryIds.includes(category.id) ? 'selected' : ''}`} onClick={() => toggleCategory(category.id)}>
                        {category.name}
                      </button>
                    ))}
                  </div>
                  {categories.some((category) => selectedCategoryIds.includes(category.id) && category.name.toLowerCase() === 'other') && (
                    <label className="mt-3 block">
                      <span className={fieldLabelClass}>Other category details</span>
                      <input className="input-shell" value={otherCategoryValue} onChange={(event) => setOtherCategoryValue(event.target.value)} placeholder="Enter the additional category" />
                    </label>
                  )}
                </div>
                <label>
                  <span className={fieldLabelClass}>Licence number</span>
                  <input className="input-shell" value={registrationForm.licenseNumber} onChange={(event) => setRegistrationForm((current) => ({ ...current, licenseNumber: event.target.value }))} placeholder="Registration or licence number" />
                </label>
                <label>
                  <span className={fieldLabelClass}>Licensing board</span>
                  <input className="input-shell" value={registrationForm.licenseBoard} onChange={(event) => setRegistrationForm((current) => ({ ...current, licenseBoard: event.target.value }))} placeholder="Licensing board, authority, or regulator" />
                </label>
                <label>
                  <span className={fieldLabelClass}>Years of experience</span>
                  <input className="input-shell" type="number" value={registrationForm.yearsOfExperience} onChange={(event) => setRegistrationForm((current) => ({ ...current, yearsOfExperience: Number(event.target.value) }))} placeholder="3" />
                </label>
                <label>
                  <span className={fieldLabelClass}>Specialty or focus area</span>
                  <input className="input-shell" value={registrationForm.specialty} onChange={(event) => setRegistrationForm((current) => ({ ...current, specialty: event.target.value }))} placeholder="Specialty, discipline, or focus area" />
                </label>
              </div>
            </details>

            <div className="mt-4">{renderRequiredDocuments()}</div>
            <button className="primary-action mt-5" disabled={saving}>{saving ? 'Saving...' : 'Create profile'}</button>
          </form>
        ) : !profile && activeStep === 'documents' ? (
          <div className="space-y-6">
            <div className="surface-card p-6">
              <h2 className="section-title">Document requirements</h2>
              <p className="section-copy mt-2">Create your profile first, then upload the documents required for your selected categories.</p>
              <div className="mt-5">{renderRequiredDocuments()}</div>
            </div>
              <div className="surface-card p-6">
                <h2 className="section-title">Document uploads</h2>
                <p className="section-copy mt-2">Document uploads unlock after your core profile has been created.</p>
                <div className="mt-5 grid gap-4">
                <label>
                  <span className={fieldLabelClass}>Document type</span>
                  <select className="input-shell" value={documentType} onChange={(event) => setDocumentType(event.target.value)}>
                    <option value="">Select document type</option>
                    {documentTypes.map((item) => <option key={item.slug} value={item.slug}>{item.name}</option>)}
                  </select>
                  </label>
                  <label>
                    <span className={fieldLabelClass}>Upload file</span>
                    <div className="rounded-3xl border border-dashed border-slate-300 bg-slate-50 px-4 py-4 text-sm text-slate-500">
                      Create your profile first to activate document uploads.
                    </div>
                  </label>
                  <button className="primary-action" type="button" disabled>Create your profile to enable uploads</button>
                </div>
              </div>
          </div>
        ) : (
          <div className="space-y-6">
            <form className={`surface-card p-6 ${activeStep === 'profile' ? '' : 'hidden'}`} onSubmit={updateProfile}>
              <h2 className="section-title">Profile</h2>
              <p className="section-copy mt-2">Verification status: {profile?.verificationStatus || 'Pending'}</p>

              <details className="mt-5 rounded-3xl border border-slate-200 bg-white px-4 py-4" open>
                <summary className="cursor-pointer text-lg font-semibold text-slate-900">Identity and contact</summary>
                <div className="profile-form-grid mt-4">
                  <div>
                    <CountryAutocomplete label="Nationality" value={updateForm.nationality} onChange={(value) => setUpdateForm((current) => ({ ...current, nationality: value }))} />
                  </div>
                  <label>
                    <span className={fieldLabelClass}>Email address</span>
                    <input className="input-shell" value={updateForm.emailAddress} onChange={(event) => setUpdateForm((current) => ({ ...current, emailAddress: event.target.value }))} />
                  </label>
                  <div>
                    <PhoneInput label="Phone number" countryValue={updateForm.nationality} phoneValue={updateForm.phoneNumber} onCountryChange={(value) => setUpdateForm((current) => ({ ...current, nationality: value }))} onPhoneChange={(value) => setUpdateForm((current) => ({ ...current, phoneNumber: value }))} />
                  </div>
                  <label>
                    <span className={fieldLabelClass}>National ID or passport</span>
                    <input className="input-shell" value={updateForm.nationalIdOrPassport} onChange={(event) => setUpdateForm((current) => ({ ...current, nationalIdOrPassport: event.target.value }))} placeholder="Government ID or passport number" />
                  </label>
                  <label className="md:col-span-2">
                    <span className={fieldLabelClass}>Address line</span>
                    <input className="input-shell" value={updateForm.addressLine} onChange={(event) => setUpdateForm((current) => ({ ...current, addressLine: event.target.value }))} placeholder="Street, building, suite, or district" />
                  </label>
                  <label>
                    <span className={fieldLabelClass}>City or town</span>
                    <input className="input-shell" value={updateForm.city} onChange={(event) => setUpdateForm((current) => ({ ...current, city: event.target.value }))} placeholder="City or town" />
                  </label>
                  <label>
                    <span className={fieldLabelClass}>State, province, or region</span>
                    <input className="input-shell" value={updateForm.county} onChange={(event) => setUpdateForm((current) => ({ ...current, county: event.target.value }))} placeholder="State, province, or region" />
                  </label>
                  <label>
                    <span className={fieldLabelClass}>Postal address</span>
                    <input className="input-shell" value={updateForm.postalAddress} onChange={(event) => setUpdateForm((current) => ({ ...current, postalAddress: event.target.value }))} placeholder="Postal or mailing address" />
                  </label>
                </div>
              </details>

              <details className="mt-4 rounded-3xl border border-slate-200 bg-white px-4 py-4" open>
                <summary className="cursor-pointer text-lg font-semibold text-slate-900">Registration and credentials</summary>
                <div className="profile-form-grid mt-4">
                  <div className="md:col-span-2">
                    <span className={fieldLabelClass}>Professional categories</span>
                    <div className="client-choice-grid">
                      {categories.map((category) => (
                        <button type="button" key={category.id} className={`client-choice ${selectedCategoryIds.includes(category.id) ? 'selected' : ''}`} onClick={() => toggleCategory(category.id)}>
                          {category.name}
                        </button>
                      ))}
                    </div>
                    {categories.some((category) => selectedCategoryIds.includes(category.id) && category.name.toLowerCase() === 'other') && (
                      <label className="mt-3 block">
                        <span className={fieldLabelClass}>Other category details</span>
                        <input className="input-shell" value={otherCategoryValue} onChange={(event) => setOtherCategoryValue(event.target.value)} />
                      </label>
                    )}
                  </div>
                  <label>
                    <span className={fieldLabelClass}>Licence number</span>
                    <input className="input-shell" value={updateForm.licenseNumber} onChange={(event) => setUpdateForm((current) => ({ ...current, licenseNumber: event.target.value }))} />
                  </label>
                  <label>
                    <span className={fieldLabelClass}>Licensing board</span>
                    <input className="input-shell" value={updateForm.licenseBoard} onChange={(event) => setUpdateForm((current) => ({ ...current, licenseBoard: event.target.value }))} placeholder="Licensing board, authority, or regulator" />
                  </label>
                  <label>
                    <span className={fieldLabelClass}>Licence expiry date</span>
                    <input className="input-shell" type="date" value={updateForm.licenseExpiryDate} onChange={(event) => setUpdateForm((current) => ({ ...current, licenseExpiryDate: event.target.value }))} />
                  </label>
                  <label>
                    <span className={fieldLabelClass}>Specialty or focus area</span>
                    <input className="input-shell" value={updateForm.specialty} onChange={(event) => setUpdateForm((current) => ({ ...current, specialty: event.target.value }))} />
                  </label>
                </div>
              </details>

              <details className="mt-4 rounded-3xl border border-slate-200 bg-white px-4 py-4" open>
                <summary className="cursor-pointer text-lg font-semibold text-slate-900">Work preferences</summary>
                <div className="profile-form-grid mt-4">
                  <label>
                    <span className={fieldLabelClass}>Years of experience</span>
                    <input className="input-shell" type="number" value={updateForm.yearsOfExperience} onChange={(event) => setUpdateForm((current) => ({ ...current, yearsOfExperience: Number(event.target.value) }))} />
                  </label>
                  <label>
                    <span className={fieldLabelClass}>Current position</span>
                    <input className="input-shell" value={updateForm.currentPosition} onChange={(event) => setUpdateForm((current) => ({ ...current, currentPosition: event.target.value }))} placeholder="Current or target title" />
                  </label>
                  <label>
                    <span className={fieldLabelClass}>Current employer</span>
                    <input className="input-shell" value={updateForm.currentEmployer} onChange={(event) => setUpdateForm((current) => ({ ...current, currentEmployer: event.target.value }))} placeholder="Current employer or independent practice" />
                  </label>
                  <label>
                    <span className={fieldLabelClass}>Preferred location</span>
                    <select className="input-shell" value={updateForm.preferredLocation} onChange={(event) => setUpdateForm((current) => ({ ...current, preferredLocation: event.target.value }))}>
                      <option value="">Select work location preference</option>
                      {preferredLocationOptions.map((item) => <option key={item} value={item}>{item}</option>)}
                    </select>
                  </label>
                  <label>
                    <span className={fieldLabelClass}>Relocation willingness (0-100)</span>
                    <input className="input-shell" type="number" value={updateForm.relocationWillingness} onChange={(event) => setUpdateForm((current) => ({ ...current, relocationWillingness: Number(event.target.value) }))} />
                  </label>
                  <label>
                    <span className={fieldLabelClass}>Expected salary</span>
                    <input className="input-shell" type="number" value={updateForm.expectedSalary} onChange={(event) => setUpdateForm((current) => ({ ...current, expectedSalary: event.target.value }))} />
                  </label>
                  <label>
                    <span className={fieldLabelClass}>Availability type</span>
                    <select className="input-shell" value={updateForm.availabilityType} onChange={(event) => setUpdateForm((current) => ({ ...current, availabilityType: event.target.value }))}>
                      {availabilityOptions.map((item) => <option key={item} value={item}>{item}</option>)}
                    </select>
                  </label>
                </div>
              </details>

              <button className="primary-action mt-5" disabled={saving}>{saving ? 'Saving...' : 'Save profile updates'}</button>
            </form>

            <div className={`space-y-6 ${activeStep === 'education' ? '' : 'hidden'}`}>
              <div className="surface-card p-6">
                <h2 className="section-title">Education</h2>
                <p className="section-copy mt-2">Add every programme separately so employers can review the full timeline clearly.</p>
                <div className="entry-stage mt-5">
                  <form onSubmit={addEducation}>
                    <div className="profile-form-grid">
                      <label><span className={fieldLabelClass}>Institution</span><input className="input-shell" value={educationForm.institution} onChange={(event) => setEducationForm((current) => ({ ...current, institution: event.target.value }))} placeholder="Institution or training provider" /></label>
                      <label><span className={fieldLabelClass}>Award</span><input className="input-shell" value={educationForm.award} onChange={(event) => setEducationForm((current) => ({ ...current, award: event.target.value }))} placeholder="Degree, diploma, certificate, or course" /></label>
                      <label><span className={fieldLabelClass}>Field of study</span><input className="input-shell" value={educationForm.fieldOfStudy} onChange={(event) => setEducationForm((current) => ({ ...current, fieldOfStudy: event.target.value }))} placeholder="Field of study or specialization" /></label>
                      <label><span className={fieldLabelClass}>Grade or result</span><input className="input-shell" value={educationForm.grade} onChange={(event) => setEducationForm((current) => ({ ...current, grade: event.target.value }))} placeholder="Optional" /></label>
                      <label><span className={fieldLabelClass}>Start date</span><input className="input-shell" type="date" value={educationForm.startDate} onChange={(event) => setEducationForm((current) => ({ ...current, startDate: event.target.value }))} /></label>
                      <label><span className={fieldLabelClass}>End date</span><input className="input-shell" type="date" value={educationForm.endDate} onChange={(event) => setEducationForm((current) => ({ ...current, endDate: event.target.value }))} /></label>
                    </div>
                    <button className="primary-action mt-5" type="submit">Add education record</button>
                  </form>
                  <div className="entry-list">
                    {education.length === 0 && <div className="entry-card"><p className="entry-card-subtitle">No education entries yet. Add each programme separately.</p></div>}
                    {education.map((item) => (
                      <div key={item.id} className="entry-card">
                        <div className="entry-card-header">
                          <div>
                            <p className="entry-card-title">{item.award}</p>
                            <p className="entry-card-subtitle">{item.institution}</p>
                          </div>
                          {item.fieldOfStudy && <span className="pill-chip">{item.fieldOfStudy}</span>}
                        </div>
                        <p className="mt-2 text-sm text-slate-600">{[item.startDate?.slice(0, 10), item.endDate?.slice(0, 10) || 'In progress'].filter(Boolean).join(' to ')}</p>
                        {item.grade && <p className="mt-2 text-sm text-slate-600">Result: {item.grade}</p>}
                      </div>
                    ))}
                  </div>
                </div>
              </div>

              <div className="surface-card p-6">
                <h2 className="section-title">Qualifications and licences</h2>
                <p className="section-copy mt-2">Capture each licence, board registration, or specialist credential as its own record.</p>
                <div className="entry-stage mt-5">
                  <form onSubmit={addQualification}>
                    <div className="profile-form-grid">
                      <label><span className={fieldLabelClass}>Qualification title</span><input className="input-shell" value={qualificationForm.title} onChange={(event) => setQualificationForm((current) => ({ ...current, title: event.target.value }))} placeholder="Qualification or licence title" /></label>
                      <label><span className={fieldLabelClass}>Issuing body</span><input className="input-shell" value={qualificationForm.issuingBody} onChange={(event) => setQualificationForm((current) => ({ ...current, issuingBody: event.target.value }))} placeholder="Board, institution, or authority" /></label>
                      <label><span className={fieldLabelClass}>Licence number</span><input className="input-shell" value={qualificationForm.licenseNumber} onChange={(event) => setQualificationForm((current) => ({ ...current, licenseNumber: event.target.value }))} placeholder="Registration or credential number" /></label>
                      <label><span className={fieldLabelClass}>Issued on</span><input className="input-shell" type="date" value={qualificationForm.issuedOn} onChange={(event) => setQualificationForm((current) => ({ ...current, issuedOn: event.target.value }))} /></label>
                      <label><span className={fieldLabelClass}>Expires on</span><input className="input-shell" type="date" value={qualificationForm.expiresOn} onChange={(event) => setQualificationForm((current) => ({ ...current, expiresOn: event.target.value }))} /></label>
                    </div>
                    <button className="primary-action mt-5" type="submit">Add qualification</button>
                  </form>
                  <div className="entry-list">
                    {qualifications.length === 0 && <div className="entry-card"><p className="entry-card-subtitle">No qualifications added yet.</p></div>}
                    {qualifications.map((item) => (
                      <div key={item.id} className="entry-card">
                        <div className="entry-card-header">
                          <div>
                            <p className="entry-card-title">{item.title}</p>
                            <p className="entry-card-subtitle">{item.issuingBody}</p>
                          </div>
                          {item.licenseNumber && <span className="pill-chip">{item.licenseNumber}</span>}
                        </div>
                        <p className="mt-2 text-sm text-slate-600">{[item.issuedOn?.slice(0, 10), item.expiresOn?.slice(0, 10) ? `Expires ${item.expiresOn.slice(0, 10)}` : null].filter(Boolean).join(' · ')}</p>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </div>

            <div className={`surface-card p-6 ${activeStep === 'experience' ? '' : 'hidden'}`}>
              <h2 className="section-title">Job experience</h2>
              <p className="section-copy mt-2">Build your work history one role at a time so employers can evaluate progression, scope, and continuity.</p>
              <div className="entry-stage mt-5">
                <form onSubmit={addExperience}>
                  <div className="profile-form-grid">
                    <label><span className={fieldLabelClass}>Employer name</span><input className="input-shell" value={experienceForm.employerName} onChange={(event) => setExperienceForm((current) => ({ ...current, employerName: event.target.value }))} placeholder="Organization or practice" /></label>
                    <label><span className={fieldLabelClass}>Job title</span><input className="input-shell" value={experienceForm.jobTitle} onChange={(event) => setExperienceForm((current) => ({ ...current, jobTitle: event.target.value }))} placeholder="Role title" /></label>
                    <label><span className={fieldLabelClass}>Employment type</span><select className="input-shell" value={experienceForm.employmentType} onChange={(event) => setExperienceForm((current) => ({ ...current, employmentType: event.target.value }))}>{employmentTypes.map((item) => <option key={item} value={item}>{item}</option>)}</select></label>
                    <label><span className={fieldLabelClass}>Location</span><input className="input-shell" value={experienceForm.location} onChange={(event) => setExperienceForm((current) => ({ ...current, location: event.target.value }))} placeholder="City, region, or remote" /></label>
                    <label><span className={fieldLabelClass}>Start date</span><input className="input-shell" type="date" value={experienceForm.startDate} onChange={(event) => setExperienceForm((current) => ({ ...current, startDate: event.target.value }))} /></label>
                    <label><span className={fieldLabelClass}>End date</span><input className="input-shell" type="date" value={experienceForm.endDate} disabled={experienceForm.isCurrentRole} onChange={(event) => setExperienceForm((current) => ({ ...current, endDate: event.target.value }))} /></label>
                    <label className="md:col-span-2 switch-card"><input type="checkbox" checked={experienceForm.isCurrentRole} onChange={(event) => setExperienceForm((current) => ({ ...current, isCurrentRole: event.target.checked, endDate: event.target.checked ? '' : current.endDate }))} /> This is my current role</label>
                    <label className="md:col-span-2"><span className={fieldLabelClass}>Responsibilities and achievements</span><textarea className="text-shell" value={experienceForm.responsibilities} onChange={(event) => setExperienceForm((current) => ({ ...current, responsibilities: event.target.value }))} placeholder="Scope, patient volume, systems used, leadership, outcomes, or notable delivery." /></label>
                  </div>
                  <button className="primary-action mt-5" type="submit">Add experience</button>
                </form>
                <div className="entry-list">
                  {experiences.length === 0 && <div className="entry-card"><p className="entry-card-subtitle">No experience entries yet.</p></div>}
                  {experiences.map((item) => (
                    <div key={item.id} className="entry-card">
                      <div className="entry-card-header">
                        <div>
                          <p className="entry-card-title">{item.jobTitle}</p>
                          <p className="entry-card-subtitle">{item.employerName}</p>
                        </div>
                        <span className="pill-chip">{item.isCurrentRole ? 'Current role' : item.employmentType || 'Experience'}</span>
                      </div>
                      <p className="mt-2 text-sm text-slate-600">{[item.location || 'Location not specified', item.startDate?.slice(0, 10), item.endDate?.slice(0, 10) || 'Present'].filter(Boolean).join(' · ')}</p>
                      {item.responsibilities && <p className="mt-3 text-sm text-slate-600">{item.responsibilities}</p>}
                    </div>
                  ))}
                </div>
              </div>
            </div>

            <form className={`surface-card p-6 ${activeStep === 'skills' ? '' : 'hidden'}`} onSubmit={updateProfile}>
              <h2 className="section-title">Skills and narrative</h2>
              <div className="profile-form-grid mt-5">
                <label className="md:col-span-2"><span className={fieldLabelClass}>Core skills</span><textarea className="text-shell" value={updateForm.skills} onChange={(event) => setUpdateForm((current) => ({ ...current, skills: event.target.value }))} placeholder="Clinical systems, procedures, care pathways, leadership, research, equipment, or patient populations." /></label>
                <div>
                  <LanguageMultiSelect label="Languages" value={updateForm.languages} onChange={(value) => setUpdateForm((current) => ({ ...current, languages: value }))} />
                </div>
                <label><span className={fieldLabelClass}>Work permit status</span><input className="input-shell" value={updateForm.workPermitStatus} onChange={(event) => setUpdateForm((current) => ({ ...current, workPermitStatus: event.target.value }))} placeholder="Citizen, resident, permit holder, or sponsorship required" /></label>
                <label className="md:col-span-2"><span className={fieldLabelClass}>Professional bio</span><textarea className="text-shell" value={updateForm.bio} onChange={(event) => setUpdateForm((current) => ({ ...current, bio: event.target.value }))} placeholder="Summarize your background, strengths, preferred environments, and what you want next." /></label>
              </div>
              <button className="primary-action mt-5" disabled={saving}>{saving ? 'Saving...' : 'Save skills and bio'}</button>
            </form>

            <div className={`space-y-6 ${activeStep === 'documents' ? '' : 'hidden'}`}>
              <div className="surface-card p-6">
                <h2 className="section-title">Document requirements</h2>
                <p className="section-copy mt-2">These requirements update when your selected professional categories change.</p>
                <div className="mt-5">{renderRequiredDocuments()}</div>
              </div>

              <div className="surface-card p-6">
                <h2 className="section-title">Document uploads</h2>
                <div className="mt-5 grid gap-4">
                  <label>
                    <span className={fieldLabelClass}>Document type</span>
                    <select className="input-shell" value={documentType} onChange={(event) => setDocumentType(event.target.value)}>
                      <option value="">Select document type</option>
                      {documentTypes.map((item) => <option key={item.slug} value={item.slug}>{item.name}</option>)}
                    </select>
                  </label>
                  <label>
                    <span className={fieldLabelClass}>Upload file</span>
                    <div className="rounded-3xl border border-dashed border-slate-300 bg-slate-50 px-4 py-4 transition hover:border-slate-400 hover:bg-white">
                      <input className="hidden" id="professional-document-upload" type="file" onChange={(event) => setDocumentFile(event.target.files?.[0] || null)} />
                      <label htmlFor="professional-document-upload" className="block cursor-pointer">
                        <span className="block text-sm font-semibold text-slate-900">{documentFile ? documentFile.name : 'Choose or drop a document here'}</span>
                        <span className="mt-1 block text-xs text-slate-500">
                          {documentFile
                            ? `${Math.max(documentFile.size / (1024 * 1024), 0.01).toFixed(2)} MB selected`
                            : 'Upload the exact document requested for your selected category and verification flow.'}
                        </span>
                      </label>
                    </div>
                  </label>
                  {selectedDocumentType && (
                    <div className="rounded-3xl bg-slate-50 px-4 py-4 text-sm text-slate-600">
                      <div className="flex flex-wrap gap-2">
                        <span className="pill-chip">{selectedDocumentType.name}</span>
                        {parseAllowedExtensions(selectedDocumentType.allowedExtensions).length > 0 && (
                          <span className="pill-chip">Extensions: {parseAllowedExtensions(selectedDocumentType.allowedExtensions).join(', ')}</span>
                        )}
                        {selectedDocumentType.maxFileSizeMb && selectedDocumentType.maxFileSizeMb > 0 && (
                          <span className="pill-chip">Max size: {selectedDocumentType.maxFileSizeMb} MB</span>
                        )}
                      </div>
                      {selectedDocumentType.description && <p className="mt-3">{selectedDocumentType.description}</p>}
                    </div>
                  )}
                  <button className="primary-action" type="button" onClick={uploadDocument} disabled={!documentFile || !documentType || saving}>Upload document</button>
                </div>
                <div className="mt-4 space-y-3">
                  {documents.map((item) => (
                    <div key={item.id} className="rounded-2xl bg-slate-50 px-4 py-4 text-sm text-slate-600">
                      <div className="flex items-center justify-between">
                        <span className="font-semibold text-slate-900">{item.type}</span>
                        <span className="pill-chip">{item.status}</span>
                      </div>
                      <p className="mt-1">{item.fileName}</p>
                      {item.verificationNotes && <p className="mt-2 text-xs text-slate-400">{item.verificationNotes}</p>}
                    </div>
                  ))}
                </div>
              </div>
            </div>

            <form className={`surface-card p-6 ${activeStep === 'security' ? '' : 'hidden'}`} onSubmit={updatePassword}>
              <h2 className="section-title">Account security</h2>
              <p className="section-copy">Rotate your password for both the public client and the admin consoles.</p>
              {passwordMessage && <div className="mt-4 rounded-2xl bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-700">{passwordMessage}</div>}
              <div className="mt-5 grid gap-4">
                <label><span className={fieldLabelClass}>Current password</span><input className="input-shell" type="password" value={passwordForm.currentPassword} onChange={(event) => setPasswordForm((current) => ({ ...current, currentPassword: event.target.value }))} /></label>
                <label><span className={fieldLabelClass}>New password</span><input className="input-shell" type="password" value={passwordForm.newPassword} onChange={(event) => setPasswordForm((current) => ({ ...current, newPassword: event.target.value }))} /></label>
                <label><span className={fieldLabelClass}>Confirm new password</span><input className="input-shell" type="password" value={passwordForm.confirmNewPassword} onChange={(event) => setPasswordForm((current) => ({ ...current, confirmNewPassword: event.target.value }))} /></label>
                <button className="primary-action" type="submit" disabled={saving}>{saving ? 'Updating...' : 'Update password'}</button>
              </div>
            </form>
          </div>
        )}
      </section>
    </Layout>
  );
}
