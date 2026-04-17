/* --- DRAG AND DROP LOGICA --- */
function allowDrop(ev) {
    ev.preventDefault();
}

function drag(ev) {
    ev.dataTransfer.setData("text", ev.target.id);
}

function drop(ev) {
    ev.preventDefault();
    const data = ev.dataTransfer.getData("text");
    const draggedElement = document.getElementById(data);
    const target = ev.target;
    const dropZone = target.closest(".drop-target");
    const pool = target.closest(".driver-list");

    /* Terugzetten naar de pool */
    if (pool && data.startsWith("clone-")) {
        const originalId = data.split('-')[1];
        const originalElement = document.getElementById(originalId);
        if (originalElement) {
            originalElement.style.display = "flex";
        }
        const parentZone = draggedElement.parentNode;
        parentZone.classList.remove("filled");
        parentZone.innerHTML = "...";
        return;
    }

    /* In een drop-zone plaatsen */
    if (dropZone) {
        if (data.startsWith("clone-")) {
            const oldParent = draggedElement.parentNode;
            oldParent.innerHTML = "...";
            oldParent.classList.remove("filled");

            dropZone.innerHTML = "";
            dropZone.appendChild(draggedElement);
        }
        else {
            const clone = draggedElement.cloneNode(true);
            clone.id = "clone-" + data + "-" + dropZone.id;
            clone.setAttribute("ondragstart", "drag(event)");

            dropZone.innerHTML = "";
            dropZone.appendChild(clone);
            draggedElement.style.display = "none";
        }
        dropZone.classList.add("filled");
    }
}

/* --- VIEW SWITCHER LOGICA (Voorspellingen) --- */
function switchView(viewId) {
    const sections = document.querySelectorAll('.race-section');
    const allCards = document.querySelectorAll('.driver-card');
    const driverPool = document.getElementById('pool-drivers-wrapper');
    const teamPool = document.getElementById('pool-teams-wrapper');
    const deadlineText = document.getElementById('deadline-text');
    const deadlineBadge = document.getElementById('deadline-status');

    /* Deadlines 2026: Australië als seizoensopener */
    const raceDeadline = "Deadline Race: 01/05/2026 - 15:00";
    const seasonDeadline = "Deadline Seizoen: Start Q1 Australië (13/03/2026 - 07:00)";

    /* Alleen uitvoeren als de deadline elementen bestaan (niet op inlogpagina) */
    if (deadlineText && deadlineBadge) {
        if (viewId.startsWith('season')) {
            deadlineText.innerText = seasonDeadline;
            deadlineBadge.className = "deadline-badge d-inline-block p-2 border border-warning text-warning fw-bold";

            if (viewId === 'season-teams') {
                driverPool?.classList.add('d-none');
                teamPool?.classList.remove('d-none');
            } else {
                driverPool?.classList.remove('d-none');
                teamPool?.classList.add('d-none');
            }
        } else {
            deadlineText.innerText = raceDeadline;
            deadlineBadge.className = "deadline-badge d-inline-block p-2 border border-danger text-danger fw-bold";

            driverPool?.classList.remove('d-none');
            teamPool?.classList.add('d-none');
        }
    }

    /* Wisselen van sectie */
    sections.forEach(sec => sec.classList.add('d-none'));
    const activeSection = document.getElementById(viewId);
    if (activeSection) {
        activeSection.classList.remove('d-none');
    }

    /* Originelen herstellen */
    allCards.forEach(card => {
        if (!card.id.startsWith('clone-')) {
            card.style.display = "flex";
        }
    });

    /* Reeds ingevulde kaarten verbergen in de pool */
    if (activeSection) {
        const filledSlots = activeSection.querySelectorAll('.drop-target.filled .driver-card');
        filledSlots.forEach(clone => {
            const originalId = clone.id.split('-')[1];
            const originalElement = document.getElementById(originalId);
            if (originalElement) {
                originalElement.style.display = "none";
            }
        });
    }

    /* Tab buttons updaten */
    const buttons = document.querySelectorAll('.btn-tab');
    buttons.forEach(btn => btn.classList.remove('active'));
    const clickedBtn = Array.from(buttons).find(btn => btn.getAttribute('onclick')?.includes(viewId));
    if (clickedBtn) clickedBtn.classList.add('active');
}