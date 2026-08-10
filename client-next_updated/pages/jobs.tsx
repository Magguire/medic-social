import Link from 'next/link';
import { useRouter } from 'next/router';
import { useEffect, useMemo, useState } from 'react';
import Layout from '../components/Layout';
import { jobApi } from '../lib/jobApi';
import { useAuth } from '../lib/useAuth';
import type { Job } from '../types';

export default function JobsPage() {
  const router = useRouter();
  const { hydrated, isAuthenticated } = useAuth();
  const [jobs, setJobs] = useState<Job[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const [locationFilter, setLocationFilter] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [departmentFilter, setDepartmentFilter] = useState('');
  const [engagementTypeFilter, setEngagementTypeFilter] = useState('');
  const [verificationFilter, setVerificationFilter] = useState('');
  const [salaryMin, setSalaryMin] = useState('');
  const [salaryMax, setSalaryMax] = useState('');
  const [options, setOptions] = useState<{ categories: Array<{ name: string; slug: string }>; locations: string[]; departments: string[]; engagementTypes: Array<{ name: string; slug: string; allowsShiftPattern: boolean }>; metrics: any } | null>(null);
  const [loading, setLoading] = useState(true);
  const pageSize = 9;

  useEffect(() => {
    if (!hydrated) return;
    setSearchTerm(typeof router.query.q === 'string' ? router.query.q : '');
    setLocationFilter(typeof router.query.location === 'string' ? router.query.location : '');
    setCategoryFilter(typeof router.query.category === 'string' ? router.query.category : '');
    setDepartmentFilter(typeof router.query.department === 'string' ? router.query.department : '');
    setEngagementTypeFilter(typeof router.query.engagementType === 'string' ? router.query.engagementType : '');
  }, [hydrated, router.query.category, router.query.department, router.query.engagementType, router.query.location, router.query.q]);

  useEffect(() => {
    if (!hydrated) return;
    jobApi.getSearchOptions().then(setOptions).catch(() => setOptions(null));
  }, [hydrated]);

  useEffect(() => {
    if (!hydrated) return;
    setLoading(true);
    jobApi.listJobs(undefined, page, pageSize, {
      q: searchTerm,
      category: categoryFilter,
      department: departmentFilter,
      engagementType: engagementTypeFilter,
      location: locationFilter,
      requireVerifiedProfessional: verificationFilter,
      salaryMin,
      salaryMax,
    })
      .then((response) => {
        setJobs(response.jobs);
        setTotalCount(response.totalCount);
      })
      .finally(() => setLoading(false));
  }, [categoryFilter, departmentFilter, engagementTypeFilter, hydrated, locationFilter, page, salaryMax, salaryMin, searchTerm, verificationFilter]);

  const filteredJobs = useMemo(() => jobs, [jobs]);

  const clearFilters = () => {
    setSearchTerm('');
    setLocationFilter('');
    setCategoryFilter('');
    setDepartmentFilter('');
    setEngagementTypeFilter('');
    setVerificationFilter('');
    setSalaryMin('');
    setSalaryMax('');
    setPage(1);
  };

  const pageStart = totalCount === 0 ? 0 : ((page - 1) * pageSize) + 1;
  const pageEnd = Math.min(page * pageSize, totalCount);

  return (
    <Layout>
      <section className="surface-card overflow-hidden">
        <div className="bg-[linear-gradient(115deg,var(--client-secondary),var(--client-primary))] px-6 py-8 text-white sm:px-8">
          <p className="text-sm font-semibold uppercase tracking-[0.32em] text-white/70">Marketplace</p>
          <h1 className="mt-3 text-4xl font-black tracking-tight">Open healthcare opportunities</h1>
          <p className="mt-3 max-w-3xl text-white/82">Browse live roles without signing in. Sign in when you are ready to apply, watch jobs, or manage your hiring workflow.</p>
          <div className="mt-6 grid gap-3 md:grid-cols-4">
            <div className="rounded-2xl bg-white/10 px-4 py-3">
              <p className="text-xs uppercase tracking-[0.2em] text-white/70">Live jobs</p>
              <p className="mt-1 text-2xl font-black">{options?.metrics?.totalPublishedJobs || totalCount}</p>
            </div>
            <div className="rounded-2xl bg-white/10 px-4 py-3">
              <p className="text-xs uppercase tracking-[0.2em] text-white/70">Locations</p>
              <p className="mt-1 text-2xl font-black">{options?.metrics?.locationCount || 0}</p>
            </div>
            <div className="rounded-2xl bg-white/10 px-4 py-3">
              <p className="text-xs uppercase tracking-[0.2em] text-white/70">Categories</p>
              <p className="mt-1 text-2xl font-black">{options?.metrics?.categoryCount || 0}</p>
            </div>
            <div className="rounded-2xl bg-white/10 px-4 py-3">
              <p className="text-xs uppercase tracking-[0.2em] text-white/70">Ready-to-apply roles</p>
              <p className="mt-1 text-2xl font-black">{options?.metrics?.verifiedRequiredJobs || 0}</p>
            </div>
          </div>
          {!isAuthenticated && (
            <div className="mt-4 flex flex-wrap gap-3">
              <Link href="/register" className="primary-action bg-white text-[var(--client-primary)] hover:bg-white/90">Create professional account</Link>
              <Link href="/login" className="secondary-action border-white/25 bg-white/10 text-white hover:bg-white/20">Employer or admin login</Link>
            </div>
          )}
        </div>
      </section>

      <details className="filter-shell mt-6" open>
        <summary>Search and filter jobs</summary>
        <div className="filter-body">
          <div className="grid gap-3 md:grid-cols-[1.2fr_1fr_1fr]">
            <input className="input-shell" value={searchTerm} onChange={(event) => { setSearchTerm(event.target.value); setPage(1); }} placeholder="Search title, facility, department, description" />
            <select className="input-shell" value={categoryFilter} onChange={(event) => { setCategoryFilter(event.target.value); setPage(1); }}>
              <option value="">All categories</option>
              {options?.categories?.map((category) => <option key={category.slug} value={category.name}>{category.name}</option>)}
            </select>
            <select className="input-shell" value={departmentFilter} onChange={(event) => { setDepartmentFilter(event.target.value); setPage(1); }}>
              <option value="">All departments</option>
              {options?.departments?.map((department) => <option key={department} value={department}>{department}</option>)}
            </select>
          </div>
          <div className="mt-3 grid gap-3 md:grid-cols-[1fr_1fr_1fr_1fr_1fr_auto]">
            <select className="input-shell" value={engagementTypeFilter} onChange={(event) => { setEngagementTypeFilter(event.target.value); setPage(1); }}>
              <option value="">All job types</option>
              {options?.engagementTypes?.map((item) => <option key={item.slug} value={item.name}>{item.name}</option>)}
            </select>
            <select className="input-shell" value={locationFilter} onChange={(event) => { setLocationFilter(event.target.value); setPage(1); }}>
              <option value="">All locations</option>
              {options?.locations?.map((location) => <option key={location} value={location}>{location}</option>)}
            </select>
            <select className="input-shell" value={verificationFilter} onChange={(event) => { setVerificationFilter(event.target.value); setPage(1); }}>
              <option value="">Any verification rule</option>
              <option value="true">Verified professionals required</option>
              <option value="false">Verification optional</option>
            </select>
            <input className="input-shell" type="number" value={salaryMin} onChange={(event) => { setSalaryMin(event.target.value); setPage(1); }} placeholder="Minimum salary" />
            <input className="input-shell" type="number" value={salaryMax} onChange={(event) => { setSalaryMax(event.target.value); setPage(1); }} placeholder="Maximum salary" />
            <button className="secondary-action h-12" onClick={clearFilters}>Clear</button>
          </div>
        </div>
      </details>

      <section className="mt-8 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {loading ? Array.from({ length: 6 }).map((_, index) => (
          <div key={index} className="surface-card animate-pulse p-6">
            <div className="h-4 w-24 rounded bg-slate-200" />
            <div className="mt-4 h-6 w-2/3 rounded bg-slate-200" />
            <div className="mt-3 h-20 rounded bg-slate-100" />
          </div>
        )) : filteredJobs.map((job) => (
          <article key={job.id} className="surface-card p-6">
            {job.posters?.find((poster) => poster.contentType?.startsWith('image/')) && (
              <img src={job.posters.find((poster) => poster.contentType?.startsWith('image/'))?.publicUrl} alt={`${job.title} poster`} className="mb-5 h-52 w-full rounded-3xl object-cover" />
            )}
            <div className="flex items-start justify-between gap-4">
              <div>
                <div className="flex flex-wrap gap-2">
                  <span className="pill-chip">{job.status}</span>
                  {job.displayStatus && job.displayStatus !== job.status && (
                    <span className="pill-chip border-amber-200 bg-amber-50 text-amber-800">
                      {job.displayStatus === 'ClosingSoon' ? 'Closing soon' : job.displayStatus}
                    </span>
                  )}
                </div>
                <h2 className="mt-4 text-2xl font-bold tracking-tight text-slate-900">{job.title}</h2>
                <p className="mt-1 text-xs font-semibold text-slate-500">{job.engagementType || 'Permanent'}{job.shiftPattern ? ` · ${job.shiftPattern}` : ''}</p>
                <p className="mt-2 text-sm text-slate-500">{job.department} · {job.location}</p>
                {job.posters?.length > 0 && <p className="mt-1 text-xs font-semibold text-slate-500">{job.posters.length} poster{job.posters.length === 1 ? '' : 's'} available</p>}
              </div>
              <div className="rounded-2xl bg-slate-50 px-3 py-2 text-right text-xs text-slate-500">
                <p>Closes</p>
                <p className="font-semibold text-slate-900">{new Date(job.closesAt).toLocaleDateString()}</p>
              </div>
            </div>
            <p className="mt-4 line-clamp-4 text-sm leading-6 text-slate-600">{job.description}</p>
            <div className="mt-5 grid gap-3 rounded-2xl bg-slate-50 p-4 text-sm text-slate-600">
              <div className="flex items-center justify-between"><span>Salary</span><strong className="text-slate-900">KES {job.salaryMin.toLocaleString()} - {job.salaryMax.toLocaleString()}</strong></div>
              <div className="flex items-center justify-between"><span>Job type</span><strong className="text-slate-900">{job.engagementType || 'Permanent'}</strong></div>
              <div className="flex items-center justify-between"><span>Category</span><strong className="text-slate-900">{job.requiredProfessionalCategory || 'Open to multiple cadres'}</strong></div>
              <div className="flex items-center justify-between"><span>Verification</span><strong className="text-slate-900">{job.requireVerifiedProfessional ? 'Required' : 'Not mandatory'}</strong></div>
            </div>
            <div className="mt-5 flex gap-3">
              <Link href={`/jobs/${job.id}`} className="primary-action flex-1">View details</Link>
            </div>
          </article>
        ))}
      </section>

      <div className="mt-8 flex items-center justify-center gap-3">
        <button className="secondary-action" disabled={page === 1} onClick={() => setPage((current) => Math.max(1, current - 1))}>Previous</button>
        <span className="pagination-meta">Showing {pageStart}-{pageEnd} of {totalCount} jobs</span>
        <span className="rounded-full bg-white px-4 py-2 text-sm font-semibold text-slate-600 shadow-sm">Page {page} of {Math.max(1, Math.ceil(totalCount / pageSize))}</span>
        <button className="secondary-action" disabled={page >= Math.ceil(totalCount / pageSize || 1)} onClick={() => setPage((current) => current + 1)}>Next</button>
      </div>
    </Layout>
  );
}
