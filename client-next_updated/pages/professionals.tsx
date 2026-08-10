import { useRouter } from 'next/router';
import { useEffect, useState } from 'react';
import Layout from '../components/Layout';
import { employerApi } from '../lib/employerApi';
import { matchingApi } from '../lib/matchingApi';
import { professionalApi } from '../lib/professionalApi';
import { socialApi, type SocialDirectoryUser } from '../lib/socialApi';
import { useAuth, useRequireAuth } from '../lib/useAuth';
import type { EmployerProfile, MatchInvitation, MatchingCandidate, ProfessionalCategory, ProfessionalProfile } from '../types';

const emptyFilters = { search: '', category: '', location: '', specialty: '', minimumYearsOfExperience: '', verificationStatus: '' };

export default function ProfessionalsDirectoryPage() {
  const router = useRouter();
  const { hydrated } = useRequireAuth();
  const { user } = useAuth();
  const [employer, setEmployer] = useState<EmployerProfile | null>(null);
  const [directory, setDirectory] = useState<ProfessionalProfile[]>([]);
  const [candidates, setCandidates] = useState<MatchingCandidate[]>([]);
  const [invites, setInvites] = useState<MatchInvitation[]>([]);
  const [categories, setCategories] = useState<ProfessionalCategory[]>([]);
  const [filters, setFilters] = useState(emptyFilters);
  const [loadingDirectory, setLoadingDirectory] = useState(false);
  const [accessError, setAccessError] = useState('');
  const [inviteMessage, setInviteMessage] = useState('Your profile matches our requirements. We would like to invite you to apply.');
  const [contactQuery, setContactQuery] = useState('');
  const [contactResults, setContactResults] = useState<SocialDirectoryUser[]>([]);
  const [contactMessage, setContactMessage] = useState('Hello, we would like to start a conversation about a healthcare opportunity.');
  const [contactBusy, setContactBusy] = useState(false);
  const [contactError, setContactError] = useState('');
  const [contactSuccess, setContactSuccess] = useState('');

  const selectedJobId = typeof router.query.jobId === 'string' ? router.query.jobId : null;

  useEffect(() => {
    if (!hydrated || !user) return;
    professionalApi.getCategories().then(setCategories).catch(() => setCategories([]));
    if (user.userType === 'Employer') {
      employerApi.getByEmail(user.email).then(async (profile) => {
        setEmployer(profile);
        if (selectedJobId) {
          const [matchedCandidates, existingInvites] = await Promise.all([
            matchingApi.getCandidates(selectedJobId, profile.tenantId),
            matchingApi.getInvites(selectedJobId, profile.tenantId),
          ]);
          setCandidates(matchedCandidates);
          setInvites(existingInvites);
        }
      }).catch(() => setEmployer(null));
    }
  }, [hydrated, selectedJobId, user]);

  useEffect(() => {
    if (!hydrated || !user) return;
    const timeout = window.setTimeout(async () => {
      setLoadingDirectory(true);
      try {
        setAccessError('');
        setDirectory(await professionalApi.listProfessionals({
          search: filters.search,
          category: filters.category,
          location: filters.location,
          specialty: filters.specialty,
          minimumYearsOfExperience: filters.minimumYearsOfExperience ? Number(filters.minimumYearsOfExperience) : undefined,
          verificationStatus: filters.verificationStatus,
        }));
      } catch (error: any) {
        setDirectory([]);
        setAccessError(error.response?.data?.errors?.[0] || error.message || 'Talent search is not available on the current subscription.');
      } finally {
        setLoadingDirectory(false);
      }
    }, 250);
    return () => window.clearTimeout(timeout);
  }, [filters, hydrated, user]);

  const sendInvite = async (professionalId: string) => {
    if (!employer || !selectedJobId) return;
    const created = await matchingApi.invite(selectedJobId, employer.tenantId, professionalId, inviteMessage);
    setInvites((current) => [created, ...current]);
  };

  const searchContact = async () => {
    setContactBusy(true);
    setContactError('');
    setContactSuccess('');
    try {
      const results = await socialApi.searchPeople(contactQuery, 'Professional');
      setContactResults(results);
      if (results.length === 0) {
        setContactError('No matching professional account was found.');
      }
    } catch (error: any) {
      setContactResults([]);
      setContactError(error.response?.data?.errors?.[0] || error.message || 'Unable to search professionals.');
    } finally {
      setContactBusy(false);
    }
  };

  const startConversation = async (person: SocialDirectoryUser) => {
    setContactBusy(true);
    setContactError('');
    setContactSuccess('');
    try {
      await socialApi.startConversation({ recipientUserId: person.userId, text: contactMessage, media: [] });
      setContactSuccess(`Conversation request sent to ${person.displayName}. They need to accept before messaging continues.`);
    } catch (error: any) {
      setContactError(error.response?.data?.errors?.[0] || error.message || 'Unable to start conversation.');
    } finally {
      setContactBusy(false);
    }
  };

  if (user?.userType !== 'Employer' && user?.userType !== 'SuperAdmin' && user?.userType !== 'Admin') {
    return <Layout><div className="surface-card p-8 text-center text-sm text-slate-500">Talent search is available to employer and admin accounts.</div></Layout>;
  }

  return (
    <Layout>
      <section className="grid gap-6 xl:grid-cols-[minmax(0,1.35fr)_minmax(340px,0.65fr)]">
        <div className="surface-card p-6">
          <h1 className="section-title">Professional directory</h1>
          <p className="section-copy">Filter the available talent pool by practical hiring criteria and review matching candidates for active roles.</p>

          <div className="mt-5 grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            <label><span className="mb-1.5 block text-sm font-semibold text-slate-700">Name, role, or keyword</span><input className="input-shell" placeholder="Search talent" value={filters.search} onChange={(event) => setFilters((current) => ({ ...current, search: event.target.value }))} /></label>
            <label><span className="mb-1.5 block text-sm font-semibold text-slate-700">Professional category</span><select className="input-shell" value={filters.category} onChange={(event) => setFilters((current) => ({ ...current, category: event.target.value }))}><option value="">All categories</option>{categories.map((item) => <option key={item.id || item.name} value={item.name}>{item.name}</option>)}</select></label>
            <label><span className="mb-1.5 block text-sm font-semibold text-slate-700">Preferred location</span><input className="input-shell" placeholder="City, region, remote" value={filters.location} onChange={(event) => setFilters((current) => ({ ...current, location: event.target.value }))} /></label>
            <label><span className="mb-1.5 block text-sm font-semibold text-slate-700">Specialty</span><input className="input-shell" placeholder="Specialty or focus area" value={filters.specialty} onChange={(event) => setFilters((current) => ({ ...current, specialty: event.target.value }))} /></label>
            <label><span className="mb-1.5 block text-sm font-semibold text-slate-700">Minimum experience</span><select className="input-shell" value={filters.minimumYearsOfExperience} onChange={(event) => setFilters((current) => ({ ...current, minimumYearsOfExperience: event.target.value }))}><option value="">Any experience</option><option value="1">1+ years</option><option value="3">3+ years</option><option value="5">5+ years</option><option value="10">10+ years</option></select></label>
            <label><span className="mb-1.5 block text-sm font-semibold text-slate-700">Verification</span><select className="input-shell" value={filters.verificationStatus} onChange={(event) => setFilters((current) => ({ ...current, verificationStatus: event.target.value }))}><option value="">Any status</option><option value="Verified">Verified</option><option value="Pending">Pending</option><option value="Rejected">Rejected</option></select></label>
          </div>
          <div className="mt-4 flex items-center justify-between gap-3 text-sm text-slate-500">
            <span>{loadingDirectory ? 'Refreshing talent results...' : `${directory.length} professional${directory.length === 1 ? '' : 's'} found`}</span>
            <button type="button" className="secondary-action" onClick={() => setFilters(emptyFilters)}>Clear filters</button>
          </div>
          {accessError && <div className="mt-4 rounded-2xl bg-amber-50 px-4 py-4 text-sm font-semibold text-amber-800">{accessError} <button className="ml-2 underline" onClick={() => router.push('/settings')}>Review plans</button></div>}

          <div className="mt-5 space-y-3">
            {directory.map((professional) => (
              <div key={professional.id || professional.userId} className="rounded-2xl border border-slate-200 bg-white px-4 py-4">
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <p className="font-semibold text-slate-900">{professional.fullName || professional.professionalCategory || 'Professional account'}</p>
                    <p className="mt-1 text-sm text-slate-500">{professional.professionalCategory || 'Profile incomplete'} · {professional.specialty || 'General profile'} · {professional.yearsOfExperience || 0} years</p>
                    <p className="mt-1 text-xs font-semibold text-slate-400">{professional.preferredLocation || professional.city || professional.county || 'Location not specified'}</p>
                  </div>
                  <span className="pill-chip">{professional.verificationStatus || 'Not started'}</span>
                </div>
              </div>
            ))}
            {!loadingDirectory && directory.length === 0 && <p className="rounded-2xl border border-dashed border-slate-200 px-4 py-8 text-center text-sm text-slate-500">No professionals match the selected filters.</p>}
          </div>
        </div>

        <div className="space-y-6">
          <div className="surface-card p-6">
            <h2 className="section-title">Start a conversation</h2>
            <p className="section-copy mt-2">Search by a known professional email address or phone number. Results are limited to protect user privacy.</p>
            <div className="mt-5 space-y-3">
              <label>
                <span className="mb-1.5 block text-sm font-semibold text-slate-700">Professional email or phone</span>
                <input className="input-shell" placeholder="name@example.com or phone number" value={contactQuery} onChange={(event) => setContactQuery(event.target.value)} onKeyDown={(event) => { if (event.key === 'Enter') searchContact(); }} />
              </label>
              <label>
                <span className="mb-1.5 block text-sm font-semibold text-slate-700">Introductory message</span>
                <textarea className="text-shell" value={contactMessage} onChange={(event) => setContactMessage(event.target.value)} />
              </label>
              <button type="button" className="primary-action" disabled={contactBusy} onClick={searchContact}>{contactBusy ? 'Checking...' : 'Find professional'}</button>
              {contactError && <p className="rounded-2xl bg-rose-50 px-4 py-3 text-sm font-semibold text-rose-700">{contactError}</p>}
              {contactSuccess && <p className="rounded-2xl bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-700">{contactSuccess}</p>}
              <div className="space-y-3">
                {contactResults.map((person) => (
                  <div key={person.userId} className="flex items-center justify-between gap-3 rounded-2xl border border-slate-200 bg-white px-4 py-4">
                    <div className="flex items-center gap-3">
                      <div className="h-12 w-12 overflow-hidden rounded-2xl bg-gradient-to-br from-emerald-200 to-amber-200 text-center text-lg font-black leading-[3rem] text-slate-900">
                        {person.avatarUrl ? <img src={person.avatarUrl} alt="" className="h-full w-full object-cover" /> : (person.displayName || person.username || 'P').slice(0, 1).toUpperCase()}
                      </div>
                      <div>
                        <p className="font-semibold text-slate-900">{person.displayName}</p>
                        <p className="text-sm text-slate-500">@{person.username} / {person.email || person.phoneNumber || 'contact verified'}</p>
                      </div>
                    </div>
                    <button type="button" className="secondary-action" disabled={contactBusy} onClick={() => startConversation(person)}>Request chat</button>
                  </div>
                ))}
              </div>
            </div>
          </div>

          <div className="surface-card p-6">
            <h2 className="section-title">Matching and invites</h2>
            <p className="section-copy mt-2">Select a job from your employer dashboard to turn on rule-based matching and invitations.</p>
            {selectedJobId && employer ? (
              <>
                <textarea className="text-shell mt-5" value={inviteMessage} onChange={(event) => setInviteMessage(event.target.value)} />
                <div className="mt-4 space-y-3">
                  {candidates.map((candidate) => (
                    <div key={candidate.professionalId} className="rounded-2xl bg-slate-50 px-4 py-4 text-sm text-slate-600">
                      <div className="flex items-start justify-between gap-4"><div><p className="font-semibold text-slate-900">{candidate.professionalCategory || 'Professional'} · {candidate.specialty || 'No specialty recorded'}</p><p className="mt-1">{candidate.yearsOfExperience} years experience</p></div><div className="text-right"><p className="text-xs uppercase tracking-[0.18em] text-slate-400">Score</p><p className="text-xl font-black text-slate-900">{candidate.score}</p></div></div>
                      <button className="primary-action mt-4" onClick={() => sendInvite(candidate.professionalId)}>Send invite</button>
                    </div>
                  ))}
                </div>
              </>
            ) : (
              <p className="mt-4 rounded-2xl border border-dashed border-slate-200 px-4 py-6 text-sm text-slate-500">Open this page from an employer job card to load matching candidates and invite controls.</p>
            )}
          </div>

          <div className="surface-card p-6">
            <h2 className="section-title">Sent invites</h2>
            <div className="mt-4 space-y-3">
              {invites.map((invite) => <div key={invite.id} className="rounded-2xl bg-slate-50 px-4 py-4 text-sm text-slate-600"><p className="font-semibold text-slate-900">Invite sent</p><p className="mt-1">{invite.message}</p></div>)}
              {invites.length === 0 && <p className="rounded-2xl border border-dashed border-slate-200 px-4 py-6 text-sm text-slate-500">No invites sent yet.</p>}
            </div>
          </div>
        </div>
      </section>
    </Layout>
  );
}
