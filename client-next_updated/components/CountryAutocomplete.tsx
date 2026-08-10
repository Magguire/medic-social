import type { ReactNode } from 'react';
import { Autocomplete, Box, InputAdornment, TextField } from '@mui/material';
import { countryOptions, findCountryOption } from '../lib/countryDirectory';

type Props = {
  label: string;
  value?: string | null;
  onChange: (value: string) => void;
};

export default function CountryAutocomplete({ label, value, onChange }: Props) {
  const selected = findCountryOption(value);
  const selectedAdornment = (
    <InputAdornment position="start">
      <Box
        component="img"
        src={selected.flagUrl}
        alt=""
        sx={{ width: 24, height: 18, borderRadius: '4px', objectFit: 'cover', boxShadow: '0 0 0 1px rgba(15, 23, 42, 0.08)' }}
      />
    </InputAdornment>
  );

  return (
    <Autocomplete
      options={countryOptions}
      value={selected || null}
      onChange={(_, nextValue) => onChange(nextValue?.name || '')}
      autoHighlight
      fullWidth
      disableClearable
      getOptionLabel={(option) => option.name}
      isOptionEqualToValue={(option, current) => option.code === current.code}
      renderOption={(props, option) => (
        <li {...props}>
          <Box
            component="img"
            src={option.flagUrl}
            alt=""
            sx={{ width: 24, height: 18, mr: 1.25, borderRadius: '4px', objectFit: 'cover', boxShadow: '0 0 0 1px rgba(15, 23, 42, 0.08)' }}
          />
          <span>{option.name}</span>
        </li>
      )}
      renderInput={(params) => (
        <TextField
          {...params}
          label={label}
          size="small"
          {...({
            InputProps: {
              ...((params as unknown as { InputProps?: Record<string, unknown> }).InputProps || {}),
              startAdornment: (
                <>
                  {selectedAdornment}
                  {((params as unknown as { InputProps?: { startAdornment?: ReactNode } }).InputProps?.startAdornment) || null}
                </>
              ),
            },
          } as Record<string, unknown>)}
        />
      )}
    />
  );
}
