'use client';
import { useEffect, useState } from 'react';
import Link from 'next/link';
import SupportScene from './SupportScene';
import { useAuth } from '@/lib/auth/AuthContext';

const areas = [
  { title: 'Health & wellbeing', symbol: '♡', text: 'Explore medical support and the evidence needed to apply.', action: 'Browse welfare programs' },
  { title: 'Learning & growth', symbol: '▤', text: 'Discover education and professional development support.', action: 'Explore available support' },
  { title: 'A helping hand', symbol: '⌂', text: 'Find assistance for unexpected events and family needs.', action: 'Find a welfare program' },
];
export default function WelfareExplorer({ role }: { role?: string }) {
  const [selected, setSelected] = useState(0); const [depth, setDepth] = useState(true);
  const { user, consumeLoginWelcome } = useAuth();
  const [welcome, setWelcome] = useState(false);
  const [welcomeCycle, setWelcomeCycle] = useState(0);
  const replayWelcome = () => { setWelcome(true); setWelcomeCycle(value => value + 1); };
  const userId = user?.id;
  useEffect(() => {
    if (userId) {
      consumeLoginWelcome(userId);
      queueMicrotask(() => { setWelcome(true); setWelcomeCycle(value => value + 1); });
    }
  }, [userId, consumeLoginWelcome]);
  useEffect(() => {
    if (!welcome) return;
    const timer = window.setTimeout(() => setWelcome(false), 6500);
    return () => window.clearTimeout(timer);
  }, [welcome, welcomeCycle]);
  const area = areas[selected];
  return <section className="welfare-explorer no-print mb-6 overflow-hidden rounded-2xl border border-red-100 bg-white p-5 sm:p-7" aria-label="Interactive welfare support explorer">
    <div className="grid items-center gap-6 md:grid-cols-2"><div><p className="text-xs font-bold uppercase tracking-widest text-red-700">People first. Support that matters.</p><h2 className="mt-2 text-2xl font-bold text-gray-900">A little support. A stronger community.</h2><p className="mt-3 max-w-xl text-sm text-gray-600">Explore the kinds of support your welfare programs may offer. Select an area to learn more.</p><div className="mt-4 flex flex-wrap gap-2" role="group" aria-label="Support areas">{areas.map((item, index) => <button key={item.title} type="button" aria-pressed={index === selected} onClick={() => setSelected(index)} className={`rounded-full border px-3 py-2 text-sm font-semibold transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-red-600 ${index === selected ? 'border-red-700 bg-red-700 text-white' : 'border-gray-200 bg-white text-gray-700 hover:bg-red-50'}`}>{item.title}</button>)}</div><div className="mt-4 min-h-24" aria-live="polite"><h3 className="font-bold text-gray-900">{area.title}</h3><p className="mt-1 text-sm text-gray-600">{area.text}</p><Link className="mt-3 inline-flex items-center gap-2 font-semibold text-red-700 underline decoration-red-200 underline-offset-4" href={role === 'Employee' ? '/employee/programs' : '/hr/reviews'}>{role === 'Employee' ? area.action : 'Open application reviews'}<span aria-hidden="true"> →</span></Link></div></div>
      <SupportScene symbol={area.symbol} depth={depth} welcome={welcome} welcomeCycle={welcomeCycle} onReplayWelcome={user ? replayWelcome : undefined} welcomeName={user?.firstName} />
      {welcome && <p className="sr-only" role="status">Welcome, {user?.firstName} {user?.lastName}. Good to have you here.</p>}
    </div><button type="button" onClick={() => setDepth(value => !value)} aria-pressed={depth} className="mt-3 rounded-lg px-2 py-1 text-xs font-medium text-gray-600 underline focus-visible:outline-2 focus-visible:outline-red-600">{depth ? 'Switch to flat 2D view' : 'Switch to 3D depth view'}</button>
  </section>;
}
