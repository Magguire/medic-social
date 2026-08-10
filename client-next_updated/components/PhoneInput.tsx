import { Box, FormControl, InputAdornment, InputLabel, MenuItem, Select, TextField } from '@mui/material';
import { countryOptions, findCountryOption } from '../lib/countryDirectory';

type Props = {
  label: string;
  countryValue?: string | null;
  phoneValue?: string | null;
  onCountryChange: (value: string) => void;
  onPhoneChange: (value: string) => void;
};

const splitPhone = (value?: string | null, fallbackDialCode?: string) => {
  if (!value) {
    return { dialCode: fallbackDialCode || '', localNumber: '' };
  }

  const parts = value.trim().split(/\s+/);
  if (parts[0]?.startsWith('+')) {
    return { dialCode: parts[0], localNumber: parts.slice(1).join(' ') };
  }

  return { dialCode: fallbackDialCode || '', localNumber: value };
};

export default function PhoneInput({ label, countryValue, phoneValue, onCountryChange, onPhoneChange }: Props) {
  const selectedCountry = findCountryOption(countryValue);
  const { dialCode, localNumber } = splitPhone(phoneValue, selectedCountry?.dialCode);

  const updateValue = (nextDialCode: string, nextLocalNumber: string) => {
    const normalizedLocalNumber = nextLocalNumber.replace(/[^\d\s()-]/g, '');
    const composed = `${nextDialCode} ${normalizedLocalNumber}`.trim();
    onPhoneChange(composed);
  };

  return (
    <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: 'minmax(0, 11rem) minmax(0, 1fr)' }, gap: 1.5, width: '100%', minWidth: 0 }}>
      <FormControl fullWidth size="small" sx={{ minWidth: 0 }}>
        <InputLabel>{label} region</InputLabel>
        <Select
          label={`${label} region`}
          value={selectedCountry.code}
          renderValue={(selectedCode) => {
            const option = countryOptions.find((item) => item.code === selectedCode) || selectedCountry;
            return (
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Box
                  component="img"
                  src={option.flagUrl}
                  alt=""
                  sx={{ width: 22, height: 16, borderRadius: '4px', objectFit: 'cover', boxShadow: '0 0 0 1px rgba(15, 23, 42, 0.08)' }}
                />
                <span>{option.dialCode}</span>
              </Box>
            );
          }}
          onChange={(event) => {
            const nextCountry = countryOptions.find((item) => item.code === event.target.value);
            onCountryChange(nextCountry?.name || '');
            updateValue(nextCountry?.dialCode || '', localNumber);
          }}
        >
          {countryOptions.map((option) => (
            <MenuItem key={option.code} value={option.code}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.25 }}>
                <Box
                  component="img"
                  src={option.flagUrl}
                  alt=""
                  sx={{ width: 22, height: 16, borderRadius: '4px', objectFit: 'cover', boxShadow: '0 0 0 1px rgba(15, 23, 42, 0.08)' }}
                />
                <span>{option.name} ({option.dialCode})</span>
              </Box>
            </MenuItem>
          ))}
        </Select>
      </FormControl>
      <TextField
        label={label}
        size="small"
        fullWidth
        sx={{ minWidth: 0 }}
        value={localNumber}
        onChange={(event) => updateValue(dialCode, event.target.value)}
        placeholder="Enter local number"
        slotProps={{
          input: {
            startAdornment: dialCode ? <InputAdornment position="start">{dialCode}</InputAdornment> : undefined,
          },
        }}
      />
    </Box>
  );
}
