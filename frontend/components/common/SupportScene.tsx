'use client';

import { useState, type CSSProperties } from 'react';

function Heart({ filled = false }: { filled?: boolean }) {
  return <svg viewBox="0 0 32 32" fill={filled ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth="2.2"><path d="M16 27S4 19.5 4 10.8C4 4.8 12 2.5 16 9c4-6.5 12-4.2 12 1.8C28 19.5 16 27 16 27Z" /></svg>;
}

export default function SupportScene({ symbol = '♡', depth = true, variant = 'dashboard', welcomeName, welcome = false, welcomeCycle = 0, onReplayWelcome }: {
  symbol?: string; depth?: boolean; variant?: 'dashboard' | 'auth'; welcomeName?: string; welcome?: boolean; welcomeCycle?: number; onReplayWelcome?: () => void;
}) {
  const [paused, setPaused] = useState(false);
  return <div className={`support-art support-art-${variant}`} data-paused={paused}>
    <div className={`support-scene ${welcome ? 'support-welcome' : ''}`} data-depth={depth} aria-hidden="true" onMouseEnter={onReplayWelcome}>
      <div className="support-orbit-plane"><div className="support-orbit-ring">{Array.from({ length: 6 }, (_, index) => <span key={index} className="support-orbit-node" style={{ '--heart-angle': `${index * 60}deg` } as CSSProperties}><span className="support-orbit-heart"><Heart filled={index % 2 === 0} /></span></span>)}</div></div>
      <div className="support-platform" />
      <div className="support-tile support-tile-back"><span>✓</span><i /><i /><i /></div>
      <div className="support-flip"><div key={welcomeCycle} className="support-flip-inner">
        <div className="support-tile support-tile-front"><span className="support-emblem">{symbol === '♡' ? <Heart /> : symbol}</span><div className="support-tile-label">Welfare support</div><div className="support-pill">Together, we thrive</div></div>
        <div className="support-welcome-face"><span className="support-welcome-heart"><Heart /></span><span className="support-welcome-kicker">Welcome,</span><strong className="support-welcome-name">{welcomeName}</strong><span className="support-welcome-caption">Good to have you here.</span></div>
      </div></div>
      <div className="support-spark spark-one">+</div><div className="support-spark spark-two">+</div><div className="support-spark spark-three">•</div>
    </div>
    <button type="button" className="support-motion-control" aria-pressed={paused} onClick={() => setPaused(value => !value)}>{paused ? 'Resume heart animation' : 'Pause heart animation'}</button>
    {onReplayWelcome && <button type="button" className="support-motion-control" onClick={onReplayWelcome}>Replay welcome</button>}
  </div>;
}
