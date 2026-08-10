import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import type { ProfessionalProfile } from '../types';

interface ProfessionalState {
  profile: ProfessionalProfile | null;
  professionals: ProfessionalProfile[];
  isLoading: boolean;
  error: string | null;
}

const initialState: ProfessionalState = {
  profile: null,
  professionals: [],
  isLoading: false,
  error: null,
};

const professionalSlice = createSlice({
  name: 'professionals',
  initialState,
  reducers: {
    setLoading: (state, action: PayloadAction<boolean>) => {
      state.isLoading = action.payload;
    },
    setError: (state, action: PayloadAction<string | null>) => {
      state.error = action.payload;
    },
    setProfile: (state, action: PayloadAction<ProfessionalProfile | null>) => {
      state.profile = action.payload;
    },
    setProfessionals: (state, action: PayloadAction<ProfessionalProfile[]>) => {
      state.professionals = action.payload;
    },
    updateProfile: (state, action: PayloadAction<Partial<ProfessionalProfile>>) => {
      if (state.profile) {
        state.profile = { ...state.profile, ...action.payload };
      }
    },
  },
});

export const { setLoading, setError, setProfile, setProfessionals, updateProfile } = professionalSlice.actions;
export default professionalSlice.reducer;
