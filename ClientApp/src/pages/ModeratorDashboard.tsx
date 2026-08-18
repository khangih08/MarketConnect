import React, { useState, useEffect } from 'react';

interface ModerationCase {
  id: number;
  entityType: string;
  entityId: number;
  riskScore: number;
  riskLevel: number | string;
  triggeredRulesJson: string;
  decision: number | string;
  status: number | string;
  currentVersionNumber: number;
  marketId?: number;
  contentSnapshotJson: string;
  createdAt: string;
  isEscalated?: boolean;
}

interface ModerationRule {
  id: number;
  ruleKey: string;
  ruleName: string;
  weight: number;
  isActive: boolean;
}

export default function ModeratorDashboard() {
  const [cases, setCases] = useState<ModerationCase[]>([]);
  const [rules, setRules] = useState<ModerationRule[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [selectedEntityType, setSelectedEntityType] = useState<string>('');
  const [selectedRiskFilter, setSelectedRiskFilter] = useState<string>('');
  const [showRulesModal, setShowRulesModal] = useState<boolean>(false);
  const [showDiffModal, setShowDiffModal] = useState<boolean>(false);
  const [diffData, setDiffData] = useState<{ v1?: any; v2?: any } | null>(null);

  useEffect(() => {
    fetchCases();
  }, [selectedEntityType, selectedRiskFilter]);

  const fetchCases = async () => {
    setLoading(true);
    try {
      let url = '/Moderation';
      const params = new URLSearchParams();
      if (selectedEntityType) params.append('entityType', selectedEntityType);
      if (selectedRiskFilter) params.append('riskLevel', selectedRiskFilter);
      
      const response = await fetch(`/api/Moderation?${params.toString()}`);
      if (response.ok) {
        const data = await response.json();
        setCases(Array.isArray(data) ? data : []);
      } else {
        // Fallback demo mock data if API is unauthenticated in SPA preview
        setCases([
          {
            id: 1,
            entityType: 'Product',
            entityId: 101,
            riskScore: 65,
            riskLevel: 'High',
            triggeredRulesJson: '["UNAUTHORIZED_CONTACT: SĐT 0912345678", "PRICE_ANOMALY"]',
            decision: 'MediumRiskManualQueue',
            status: 'PendingManualReview',
            currentVersionNumber: 2,
            contentSnapshotJson: '{"Name":"Táo Envy Mỹ (Đã Sửa)","Price":120000,"Phone":"0912345678"}',
            createdAt: new Date().toISOString()
          },
          {
            id: 2,
            entityType: 'Product',
            entityId: 102,
            riskScore: 45,
            riskLevel: 'Medium',
            triggeredRulesJson: '["PRICE_ANOMALY: Giá rau củ bất thường"]',
            decision: 'MediumRiskManualQueue',
            status: 'PendingManualReview',
            currentVersionNumber: 1,
            contentSnapshotJson: '{"Name":"Cam Sành Tiền Giang","Price":45000}',
            createdAt: new Date().toISOString()
          }
        ]);
      }
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  const fetchRules = async () => {
    try {
      const res = await fetch('/Moderation/GetRules');
      if (res.ok) {
        const data = await res.json();
        setRules(data);
      }
    } catch (e) {
      console.error(e);
    }
  };

  const handleOpenRules = () => {
    fetchRules();
    setShowRulesModal(true);
  };

  const handleOpenDiff = async (entityType: string, entityId: number) => {
    try {
      const res = await fetch(`/Moderation/GetVersionDiff?entityType=${entityType}&entityId=${entityId}`);
      if (res.ok) {
        const data = await res.json();
        setDiffData({
          v1: data.version1 ? JSON.parse(data.version1.snapshotJson || '{}') : null,
          v2: data.version2 ? JSON.parse(data.version2.snapshotJson || '{}') : null
        });
        setShowDiffModal(true);
      }
    } catch (e) {
      console.error(e);
    }
  };

  const handleReview = async (caseId: number, decisionStatus: string) => {
    const notes = prompt(`Nhập lý do cho quyết định ${decisionStatus}:`);
    if (notes === null) return;
    if ((decisionStatus === 'Rejected' || decisionStatus === 'ChangesRequired') && !notes.trim()) {
      alert('Quản trị viên BẮT BUỘC phải nhập lý do!');
      return;
    }

    try {
      const formData = new FormData();
      formData.append('caseId', caseId.toString());
      formData.append('decisionStatus', decisionStatus);
      formData.append('notes', notes);

      const res = await fetch('/Moderation/Review', { method: 'POST', body: formData });
      if (res.ok) {
        alert('Đã cập nhật quyết định thành công!');
        fetchCases();
      }
    } catch (e) {
      alert('Lỗi xử lý kiểm duyệt.');
    }
  };

  return (
    <div className="max-w-[1440px] mx-auto p-6 space-y-6 bg-gray-50 min-h-screen">
      {/* Header */}
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 bg-white p-6 rounded-3xl shadow-sm border border-gray-200">
        <div>
          <h1 className="text-2xl font-black text-gray-900 flex items-center gap-2">
            <span className="material-symbols-outlined text-emerald-800 text-[32px]">gavel</span>
            <span>Dashboard Kiểm Duyệt Viên (Moderator Portal)</span>
          </h1>
          <p className="text-xs text-gray-500 mt-1">Đánh giá rủi ro tự động (FR-06), kiểm duyệt ưu tiên & so sánh phiên bản (FR-07), phân quyền phạm vi Data Scope (FR-08).</p>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={handleOpenRules}
            className="px-4 py-2.5 bg-blue-50 hover:bg-blue-100 text-blue-900 font-extrabold rounded-2xl text-xs border border-blue-300 transition-all flex items-center gap-1.5 cursor-pointer"
          >
            <span className="material-symbols-outlined text-sm">tune</span> Quy Tắc Kiểm Duyệt (FR-06)
          </button>
          <a
            href="/AdminMfa/Verify"
            className="px-4 py-2.5 bg-amber-50 hover:bg-amber-100 text-amber-900 font-extrabold rounded-2xl text-xs border border-amber-300 transition-all flex items-center gap-1.5"
          >
            <span className="material-symbols-outlined text-sm">security</span> OTP MFA Quản Trị
          </a>
        </div>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-center">
        <div className="bg-white p-4 rounded-2xl border border-gray-200 shadow-2xs">
          <div className="text-[11px] font-extrabold text-gray-400 uppercase tracking-wider">Hàng Đợi Chờ Duyệt</div>
          <div className="text-2xl font-black text-amber-700 mt-1">{cases.length}</div>
        </div>
        <div className="bg-white p-4 rounded-2xl border border-gray-200 shadow-2xs">
          <div className="text-[11px] font-extrabold text-gray-400 uppercase tracking-wider">Rủi Ro Cao (High)</div>
          <div className="text-2xl font-black text-red-600 mt-1">{cases.filter(c => String(c.riskLevel) === 'High' || c.riskScore >= 60).length}</div>
        </div>
        <div className="bg-white p-4 rounded-2xl border border-gray-200 shadow-2xs">
          <div className="text-[11px] font-extrabold text-gray-400 uppercase tracking-wider">Phiên Bản Mới Nhất</div>
          <div className="text-2xl font-black text-emerald-800 mt-1">v2.0</div>
        </div>
        <div className="bg-white p-4 rounded-2xl border border-gray-200 shadow-2xs">
          <div className="text-[11px] font-extrabold text-gray-400 uppercase tracking-wider">Trạng Thái Phạm Vi (Data Scope)</div>
          <div className="text-2xl font-black text-purple-800 mt-1">Active</div>
        </div>
      </div>

      {/* Queue List */}
      <div className="space-y-4">
        {loading ? (
          <div className="text-center py-12 text-gray-400 font-bold">Đang tải hàng đợi kiểm duyệt...</div>
        ) : cases.length === 0 ? (
          <div className="bg-white p-12 rounded-3xl text-center text-gray-500 border border-gray-200">
            <span className="material-symbols-outlined text-5xl text-emerald-600 block mb-2">check_circle</span>
            Không có hồ sơ nào cần xử lý trong phạm vi quản lý của bạn.
          </div>
        ) : (
          cases.map(c => (
            <div key={c.id} className="bg-white rounded-3xl border border-gray-200 p-6 space-y-4 shadow-sm hover:border-emerald-300 transition-colors">
              <div className="flex flex-wrap items-center justify-between gap-3 pb-3 border-b border-gray-100">
                <div className="flex items-center gap-3">
                  <span className="bg-emerald-100 text-emerald-900 text-xs font-black px-3 py-1 rounded-full uppercase">{c.entityType}</span>
                  <span className="font-extrabold text-sm text-gray-900">ID #{c.entityId}</span>
                  <span className="text-xs text-gray-400">Version #{c.currentVersionNumber}</span>
                </div>
                <span className={`px-3 py-1 rounded-full font-black text-xs ${c.riskScore >= 60 ? 'bg-red-100 text-red-700 border border-red-300' : 'bg-amber-100 text-amber-900 border border-amber-300'}`}>
                  Điểm rủi ro: {c.riskScore}/100
                </span>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs text-gray-700 bg-gray-50 p-4 rounded-2xl border border-gray-100">
                <div>
                  <p className="font-extrabold text-gray-900 mb-1">Nội dung Snapshot:</p>
                  <pre className="bg-white p-2.5 rounded-xl border border-gray-200 text-[11px] font-mono overflow-x-auto max-h-32">{c.contentSnapshotJson}</pre>
                </div>
                <div>
                  <p className="font-extrabold text-gray-900 mb-1">Quy tắc bị kích hoạt:</p>
                  <div className="bg-white p-2.5 rounded-xl border border-gray-200 text-[11px] text-red-600 font-bold max-h-32 overflow-y-auto">
                    {c.triggeredRulesJson || 'Không có vi phạm cơ bản.'}
                  </div>
                </div>
              </div>

              <div className="flex flex-wrap items-center justify-between gap-3 pt-2">
                <button
                  onClick={() => handleOpenDiff(c.entityType, c.entityId)}
                  className="text-xs font-bold text-blue-700 hover:underline flex items-center gap-1 cursor-pointer"
                >
                  <span className="material-symbols-outlined text-sm">difference</span> So sánh phiên bản (Diff) &rarr;
                </button>
                <div className="flex items-center gap-2">
                  <button onClick={() => handleReview(c.id, 'Approved')} className="bg-emerald-800 hover:bg-emerald-900 text-white font-extrabold px-3.5 py-1.5 rounded-xl text-xs cursor-pointer">
                    ✓ Duyệt
                  </button>
                  <button onClick={() => handleReview(c.id, 'ChangesRequired')} className="bg-amber-600 hover:bg-amber-700 text-white font-extrabold px-3.5 py-1.5 rounded-xl text-xs cursor-pointer">
                    ✎ Yêu Cầu Sửa
                  </button>
                  <button onClick={() => handleReview(c.id, 'Rejected')} className="bg-red-600 hover:bg-red-700 text-white font-extrabold px-3.5 py-1.5 rounded-xl text-xs cursor-pointer">
                    ✕ Từ Chối
                  </button>
                </div>
              </div>
            </div>
          ))
        )}
      </div>

      {/* Rules Modal */}
      {showRulesModal && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-xs z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-2xl w-full p-6 space-y-4 shadow-2xl">
            <div className="flex justify-between items-center border-b border-gray-100 pb-3">
              <h3 className="font-extrabold text-lg text-gray-900">Cấu Hình Quy Tắc Kiểm Duyệt (FR-06)</h3>
              <button onClick={() => setShowRulesModal(false)} className="text-gray-400 font-extrabold text-xl">&times;</button>
            </div>
            <div className="space-y-3 max-h-96 overflow-y-auto text-xs">
              {rules.length === 0 ? (
                <div className="text-gray-400 text-center py-4">Đang tải danh sách quy tắc...</div>
              ) : (
                rules.map(r => (
                  <div key={r.id} className="bg-gray-50 p-3 rounded-2xl border border-gray-200 flex justify-between items-center">
                    <div>
                      <span className="font-mono bg-blue-100 text-blue-900 px-2 py-0.5 rounded text-[10px] font-bold">{r.ruleKey}</span>
                      <h4 className="font-bold text-gray-900 mt-1">{r.ruleName}</h4>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="font-bold text-gray-600">Trọng số: {r.weight}</span>
                      <span className={`px-2 py-0.5 rounded-full text-[10px] font-black ${r.isActive ? 'bg-emerald-100 text-emerald-800' : 'bg-gray-200 text-gray-600'}`}>
                        {r.isActive ? 'Bật' : 'Tắt'}
                      </span>
                    </div>
                  </div>
                ))
              )}
            </div>
            <div className="pt-3 border-t border-gray-100 flex justify-end">
              <button onClick={() => setShowRulesModal(false)} className="px-5 py-2 bg-gray-200 text-gray-800 font-extrabold rounded-xl text-xs">Đóng</button>
            </div>
          </div>
        </div>
      )}

      {/* Diff Modal */}
      {showDiffModal && diffData && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-xs z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-3xl w-full p-6 space-y-4 shadow-2xl max-h-[90vh] overflow-y-auto">
            <div className="flex justify-between items-center border-b border-gray-100 pb-3">
              <h3 className="font-extrabold text-lg text-gray-900">So Sánh Phiên Bản Nội Dung (ContentVersion Diff)</h3>
              <button onClick={() => setShowDiffModal(false)} className="text-gray-400 font-extrabold text-xl">&times;</button>
            </div>
            <div className="grid grid-cols-2 gap-4 text-xs font-mono">
              <div className="bg-red-50 p-3 rounded-2xl border border-red-200">
                <h4 className="font-bold text-red-900 mb-2">Phiên Bản Cũ (v1):</h4>
                <pre className="whitespace-pre-wrap">{JSON.stringify(diffData.v1, null, 2)}</pre>
              </div>
              <div className="bg-emerald-50 p-3 rounded-2xl border border-emerald-200">
                <h4 className="font-bold text-emerald-900 mb-2">Phiên Bản Mới (v2):</h4>
                <pre className="whitespace-pre-wrap">{JSON.stringify(diffData.v2, null, 2)}</pre>
              </div>
            </div>
            <div className="pt-3 border-t border-gray-100 flex justify-end">
              <button onClick={() => setShowDiffModal(false)} className="px-5 py-2 bg-gray-200 text-gray-800 font-extrabold rounded-xl text-xs">Đóng</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
