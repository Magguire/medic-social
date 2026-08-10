import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { VerificationRequest } from '../types';

interface VerificationState {
  requests: VerificationRequest[];
  currentRequest: VerificationRequest | null;
  isLoading: boolean;
  error: string | null;
}

const initialState: VerificationState = {
  requests: [],
  currentRequest: null,
  isLoading: false,
  error: null,
};

const verificationSlice = createSlice({
  name: 'verification',
  initialState,
  reducers: {
    setLoading: (state, action: PayloadAction<boolean>) => {
      state.isLoading = action.payload;
    },
    setError: (state, action: PayloadAction<string | null>) => {
      state.error = action.payload;
    },
    setRequests: (state, action: PayloadAction<VerificationRequest[]>) => {
      state.requests = action.payload;
    },
    setCurrentRequest: (state, action: PayloadAction<VerificationRequest | null>) => {
      state.currentRequest = action.payload;
    },
    updateRequest: (state, action: PayloadAction<VerificationRequest>) => {
      const index = state.requests.findIndex(r => r.id === action.payload.id);
      if (index !== -1) {
        state.requests[index] = action.payload;
      }
    },
  },
});

export const { setLoading, setError, setRequests, setCurrentRequest, updateRequest } = verificationSlice.actions;
export default verificationSlice.reducer;
