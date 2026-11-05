import { statusLabel } from '@/lib/api/records';
export default function ApplicationJourney({ status }: { status: number }) {
  const stages = ['Draft', 'HR review', 'Committee', 'Management', 'Final decision'];
  const current = status === 1 ? 0 : status === 2 || status === 3 ? 1 : status === 4 ? 2 : status === 5 || status === 7 ? 3 : 4;
  const stopped = [6, 9, 10].includes(status);
  return <section className="my-5 rounded-xl border border-gray-200 bg-gray-50 p-4" aria-label="Application progress"><h2 className="mb-3 text-sm font-bold text-gray-900">Your application journey</h2>{stopped ? <p className="text-sm text-gray-800">{statusLabel(status)}. See the timeline for the recorded stage, reviewer, and reason.</p> : <ol className="grid grid-cols-2 gap-3 sm:grid-cols-5">{stages.map((stage, index) => <li key={stage} aria-current={index === current ? 'step' : undefined} className="flex items-center gap-2 text-sm"><span className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-full font-bold ${index < current || status === 8 ? 'bg-green-100 text-green-800' : index === current ? 'bg-red-700 text-white shadow-sm' : 'border border-gray-300 bg-white text-gray-500'}`}>{index < current || status === 8 ? '✓' : index + 1}</span><span className={index <= current ? 'font-semibold text-gray-900' : 'text-gray-600'}>{stage}</span></li>)}</ol>}</section>;
}
