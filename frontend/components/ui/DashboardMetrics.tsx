import Card from './Card';

export default function DashboardMetrics({ items }: { items: { label: string; value: number | undefined; hint?: string }[] }) {
  return <div className="mb-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">{items.map((item, index) => (
    <Card key={item.label} className={`border-l-4 ${['border-red-600', 'border-blue-600', 'border-green-600', 'border-amber-600'][index % 4]}`}>
      <p className="text-sm font-medium text-gray-600">{item.label}</p>
      <p className="mt-2 text-3xl font-bold text-gray-900">{item.value?.toLocaleString(undefined, { maximumFractionDigits: 2 }) ?? '—'}</p>
      {item.hint && <p className="mt-1 text-xs text-gray-500">{item.hint}</p>}
    </Card>
  ))}</div>;
}
