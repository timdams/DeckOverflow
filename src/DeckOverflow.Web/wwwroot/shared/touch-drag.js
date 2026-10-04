// touch-drag.js - HTML5-slepen werkt niet met een vinger. Dit vertaalt een vinger die een [draggable="true"]
// element versleept naar dezelfde drag-events (dragstart, dragenter, dragover, drop, dragend), zodat de shell maar
// één soort slepen kent. Een tik zonder bewegen blijft een gewone klik. Geen spelregels hier.

(() => {
  const START_PX = 10;   // zo ver moet de vinger bewegen voor het slepen is, en geen tik
  let source = null;
  let ghost = null;
  let over = null;
  let data = null;
  let startX = 0;
  let startY = 0;

  const fire = (el, type, x, y) => {
    if (!el) return;
    el.dispatchEvent(new DragEvent(type, { bubbles: true, cancelable: true, clientX: x, clientY: y, dataTransfer: data }));
  };

  const reset = () => {
    ghost?.remove();
    source = ghost = over = data = null;
  };

  document.addEventListener('touchstart', (e) => {
    if (e.touches.length !== 1) return reset();
    source = e.target.closest?.('[draggable="true"]') ?? null;
    if (!source) return;
    startX = e.touches[0].clientX;
    startY = e.touches[0].clientY;
  }, { passive: true });

  document.addEventListener('touchmove', (e) => {
    if (!source) return;
    const t = e.touches[0];
    if (!ghost) {
      if (Math.hypot(t.clientX - startX, t.clientY - startY) < START_PX) return;
      data = new DataTransfer();
      fire(source, 'dragstart', startX, startY);
      // Een kopie onder de vinger, zodat je ziet wat je sleept
      const r = source.getBoundingClientRect();
      ghost = source.cloneNode(true);
      Object.assign(ghost.style, {
        position: 'fixed', left: '0', top: '0', width: `${r.width}px`, margin: '0',
        pointerEvents: 'none', zIndex: '1000', opacity: '0.85',
      });
      document.body.appendChild(ghost);
    }
    e.preventDefault();   // tijdens het slepen scrolt de pagina niet mee
    ghost.style.transform = `translate(${t.clientX - ghost.offsetWidth / 2}px, ${t.clientY - ghost.offsetHeight - 12}px)`;
    const target = document.elementFromPoint(t.clientX, t.clientY);
    if (target !== over) {
      fire(over, 'dragleave', t.clientX, t.clientY);
      over = target;
      fire(over, 'dragenter', t.clientX, t.clientY);
    }
    fire(over, 'dragover', t.clientX, t.clientY);
  }, { passive: false });

  document.addEventListener('touchend', (e) => {
    if (!source) return;
    if (ghost) {
      const t = e.changedTouches[0];
      ghost.remove();
      fire(document.elementFromPoint(t.clientX, t.clientY), 'drop', t.clientX, t.clientY);
      fire(source, 'dragend', t.clientX, t.clientY);
      e.preventDefault();   // na een sleep volgt geen klik
    }
    reset();
  }, { passive: false });

  document.addEventListener('touchcancel', () => {
    if (source && ghost) fire(source, 'dragend', startX, startY);
    reset();
  });
})();
