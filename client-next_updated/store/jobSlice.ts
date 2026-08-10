import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { Job, JobApplication } from '../types';

interface JobState {
  jobs: Job[];
  currentJob: Job | null;
  applications: JobApplication[];
  isLoading: boolean;
  error: string | null;
  pagination: { page: number; pageSize: number; totalCount: number };
}

const initialState: JobState = {
  jobs: [],
  currentJob: null,
  applications: [],
  isLoading: false,
  error: null,
  pagination: { page: 1, pageSize: 20, totalCount: 0 },
};

const jobSlice = createSlice({
  name: 'jobs',
  initialState,
  reducers: {
    setLoading: (state, action: PayloadAction<boolean>) => {
      state.isLoading = action.payload;
    },
    setError: (state, action: PayloadAction<string | null>) => {
      state.error = action.payload;
    },
    setJobs: (state, action: PayloadAction<{ jobs: Job[]; totalCount: number }>) => {
      state.jobs = action.payload.jobs;
      state.pagination.totalCount = action.payload.totalCount;
    },
    setCurrentJob: (state, action: PayloadAction<Job | null>) => {
      state.currentJob = action.payload;
    },
    setApplications: (state, action: PayloadAction<JobApplication[]>) => {
      state.applications = action.payload;
    },
    addApplication: (state, action: PayloadAction<JobApplication>) => {
      state.applications.push(action.payload);
    },
    setPagination: (state, action: PayloadAction<{ page: number; pageSize: number }>) => {
      state.pagination.page = action.payload.page;
      state.pagination.pageSize = action.payload.pageSize;
    },
  },
});

export const { setLoading, setError, setJobs, setCurrentJob, setApplications, addApplication, setPagination } = jobSlice.actions;
export default jobSlice.reducer;
