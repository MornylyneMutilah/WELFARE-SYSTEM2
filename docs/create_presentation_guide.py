from pathlib import Path
from docx import Document
from docx.shared import Inches, Pt, RGBColor

out = Path(__file__).parent / 'Welfare_Project_Presentation_Guide.docx'
doc = Document()
for style in doc.styles:
    for border in list(style.element.iter('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}pBdr')):
        border.getparent().remove(border)
section = doc.sections[0]
section.page_width, section.page_height = Inches(8.27), Inches(11.69)
section.top_margin = section.bottom_margin = Inches(.60)
section.left_margin = section.right_margin = Inches(.70)
normal = doc.styles['Normal']
normal.font.name = 'Calibri'
normal.font.size = Pt(11)
normal.paragraph_format.space_after = Pt(5)
normal.paragraph_format.line_spacing = 1.02
title = doc.styles['Title']
title.font.name = 'Calibri'
title.font.size = Pt(19)
title.font.bold = True
title.font.color.rgb = RGBColor(0, 0, 0)
title.paragraph_format.space_after = Pt(7)
doc.add_paragraph('Welfare System Presentation Guide', 'Title')
p = doc.add_paragraph()
r = p.add_run('Project title  '); r.bold = True
p.add_run('Modernizing Organizational Welfare Programs: A Web-Based System with Dynamic Rules and Ad-Hoc Data Exploration Tools for Diverse Institutional Stakeholders')

points = [
('1  Opening and problem', 'My project centralizes welfare applications, eligibility checks, reviews and records. It addresses fragmented paperwork, unclear application progress and difficulty retrieving decisions for accountability.'),
('2  Stakeholders and workflow', 'Five roles: Employee, HR Officer, Welfare Committee Member, Institutional Management and Administrator. Demonstrate employee submission, departmental HR review, committee endorsement and management approval or rejection. Employees track progress; administrators manage the platform.'),
('3  Dynamic rules', 'Administrators configure program amount limits, minimum service months, required documents and active status without changing code. Submission checks use verified employment details and current program requirements. Rule types are predefined; this is not an unrestricted rule builder. Duplicate and frequency flags remain advisory.'),
('4  Ad hoc data exploration', 'Demonstrate date-filtered application reports; decision searches by employee or program, stage and outcome; and audit filters by action, entity, actor and date. Open individual records, export matching CSV results and print. Access follows each role. Exploration uses predefined fields, not arbitrary SQL or a drag-and-drop report designer.'),
('5  Security and accountability', 'Backend role and ownership checks; no self-review; protected privileged-role assignment; suspension invalidates sessions. Decisions retain reviewer, comments, timestamps and historical snapshots where available. Appeals and correction addenda preserve original decisions. Evidence has controlled access and document-hash checks.'),
('6  Administration and intelligent support', 'Show Users, Programs, Role requests, Decision register, Audit log, Detection controls, Budgets and Readiness. Administrators propose budgets; management decides. Amounts are commitments, not payments. Anomaly flags support investigation; they do not prove fraud. The optional amount model needs sufficient history and institutional evaluation.'),
('7  Architecture and engineering depth', 'Next.js and TypeScript frontend; Axios communicates with an ASP.NET Core .NET 10 API; PostgreSQL stores records. Transactions and database locks protect concurrent decisions. Dashboard data refreshes periodically. The most demanding work is keeping permissions, state changes, evidence and audit records consistent.'),
('8  Demonstration and closing', 'Use prepared demo accounts: configure a program; show eligibility and submission; complete the review stages; retrieve a decision and filtered export. Show evidence for tests actually run. Acknowledge limits: no chatbot implemented, social activation deferred, and production email/scanning require operational setup. Close with improved traceability, configurable administration and stakeholder access.'),
]
for heading, text in points:
    p = doc.add_paragraph()
    p.paragraph_format.keep_together = True
    r = p.add_run(heading + '\n')
    r.bold = True
    r.font.color.rgb = RGBColor.from_string('A61B32')
    p.add_run(text)
doc.core_properties.title = 'Welfare System Presentation Guide'
doc.core_properties.subject = 'One page presentation speaking guide'
doc.core_properties.author = 'Mornylyne Mutilah'
doc.save(out)
print(out)
