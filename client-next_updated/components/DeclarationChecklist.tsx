import type { DeclarationConfig } from '../lib/declarationApi';

type Props = {
  declarations: DeclarationConfig[];
  acceptedIds: string[];
  onChange: (acceptedIds: string[]) => void;
};

export function requiredDeclarationsAccepted(declarations: DeclarationConfig[], acceptedIds: string[]) {
  return declarations.filter((item) => item.isRequired).every((item) => acceptedIds.includes(item.id));
}

export default function DeclarationChecklist({ declarations, acceptedIds, onChange }: Props) {
  if (!declarations.length) {
    return null;
  }

  const toggle = (id: string) => {
    onChange(acceptedIds.includes(id) ? acceptedIds.filter((item) => item !== id) : [...acceptedIds, id]);
  };

  return (
    <div className="rounded-3xl border border-emerald-100 bg-emerald-50/60 p-4">
      <p className="text-sm font-black uppercase tracking-[0.18em] text-emerald-700">Declarations</p>
      <div className="mt-3 grid gap-3">
        {declarations.map((item) => (
          <label key={item.id} className="flex gap-3 rounded-2xl bg-white p-3 text-sm text-slate-700 shadow-sm">
            <input type="checkbox" checked={acceptedIds.includes(item.id)} onChange={() => toggle(item.id)} />
            <span>
              <strong className="block text-slate-900">{item.title}{item.isRequired ? ' *' : ''}</strong>
              {item.body}
            </span>
          </label>
        ))}
      </div>
    </div>
  );
}
