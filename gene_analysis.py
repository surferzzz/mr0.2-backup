#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Desaturase基因系统发育树分析
"""

import os
from Bio import Phylo
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
import pandas as pd
import numpy as np

# 设置中文字体
plt.rcParams['font.sans-serif'] = ['Microsoft YaHei', 'SimHei', 'Arial']
plt.rcParams['axes.unicode_minus'] = False

# 数据文件路径
tree_file = r"D:\Telegram Desktop\downloads\111\基因作业\data and R script\desaturase\desaturase_tree.nwk"
output_dir = r"D:\Telegram Desktop\downloads\111\基因作业\data and R script\desaturase"

print("=" * 60)
print("Desaturase基因系统发育树分析")
print("=" * 60)

# 读取系统发育树
print("\n正在读取系统发育树...")
tree = Phylo.read(tree_file, "newick")
print(f"[OK] 成功读取树文件")

# 获取所有末端节点
terminals = [t.name for t in tree.get_terminals()]
print(f"[OK] 共有 {len(terminals)} 个物种")

# 基因分类
z9_18_genes = [name for name in terminals if 'Z9_18' in name]
z9_16_genes = [name for name in terminals if 'Z9_16' in name]
z10_genes = [name for name in terminals if ('Z10' in name or 'Z/E10' in name or 'Z/E11' in name or 'Z11' in name)]
outgroup = [name for name in terminals if 'Tick' in name]

print(f"\n基因簇分类:")
print(f"  Z9-18系列: {len(z9_18_genes)} 个")
print(f"  Z9-16系列: {len(z9_16_genes)} 个")
print(f"  Z10/E系列: {len(z10_genes)} 个")
print(f"  外群: {len(outgroup)} 个")

def get_color(name):
    if name in z9_18_genes:
        return 'green'
    elif name in z9_16_genes:
        return 'blue'
    elif name in z10_genes:
        return 'red'
    elif name in outgroup:
        return 'black'
    return 'gray'

# 生成图表
print("\n正在生成可视化图表...")

# 图1: 基本系统树
print("  生成图1: 基本系统树...")
fig, ax = plt.subplots(figsize=(12, 10))
Phylo.draw(tree, axes=ax, do_show=False)
ax.set_title('Desaturase遺伝子系統発生樹', fontsize=16, fontweight='bold', pad=20)
ax.set_xlabel('進化距離', fontsize=12)
plt.tight_layout()
output_file = os.path.join(output_dir, '図1_基本系統樹.png')
plt.savefig(output_file, dpi=300, bbox_inches='tight')
plt.close()
print(f"  [OK] 保存完成")

# 图2: 彩色分类树
print("  生成图2: 基因簇分类...")
fig, ax = plt.subplots(figsize=(14, 12))
Phylo.draw(tree, axes=ax, do_show=False,
           label_func=lambda x: x.name if x.name else '',
           label_colors=lambda x: get_color(x) if x else 'black')
ax.set_title('Desaturase遺伝子クラスター分類\n(緑: Z9-18, 青: Z9-16, 赤: Z10/E系列, 黒: 外群)', 
             fontsize=14, fontweight='bold', pad=20)
ax.set_xlabel('進化距離', fontsize=12)

legend_elements = [
    mpatches.Patch(color='green', label=f'Z9-18デサチュラーゼ ({len(z9_18_genes)}個)'),
    mpatches.Patch(color='blue', label=f'Z9-16デサチュラーゼ ({len(z9_16_genes)}個)'),
    mpatches.Patch(color='red', label=f'Z10/E系列デサチュラーゼ ({len(z10_genes)}個)'),
    mpatches.Patch(color='black', label=f'外群Tick ({len(outgroup)}個)')
]
ax.legend(handles=legend_elements, loc='upper left', fontsize=10)
plt.tight_layout()
output_file = os.path.join(output_dir, '図2_遺伝子クラスター分類.png')
plt.savefig(output_file, dpi=300, bbox_inches='tight')
plt.close()
print(f"  [OK] 保存完成")

# 计算距离矩阵
print("\n正在计算进化距离...")
distance_matrix = {}
for t1 in tree.get_terminals():
    distance_matrix[t1.name] = {}
    for t2 in tree.get_terminals():
        dist = tree.distance(t1, t2)
        distance_matrix[t1.name][t2.name] = dist

df_dist = pd.DataFrame(distance_matrix).T
df_dist = df_dist.round(4)
output_file = os.path.join(output_dir, '種間進化距離マトリックス.csv')
df_dist.to_csv(output_file, encoding='utf-8-sig')
print(f"  [OK] 距離矩阵已保存")

# 统计分析
all_distances = []
for clade in tree.find_clades():
    if clade.branch_length is not None:
        all_distances.append(clade.branch_length)

stats = {
    '最短分支長': min(all_distances),
    '最長分支長': max(all_distances),
    '平均分支長': np.mean(all_distances),
    '総樹長': sum(all_distances)
}

# 簇内距离
def calc_intra_distance(gene_list):
    if len(gene_list) < 2:
        return 0
    distances = []
    for i, g1 in enumerate(gene_list):
        for g2 in gene_list[i+1:]:
            if g1 in df_dist.index and g2 in df_dist.columns:
                distances.append(df_dist.loc[g1, g2])
    return np.mean(distances) if distances else 0

intra_distances = {
    'Z9-18クラスター': calc_intra_distance(z9_18_genes),
    'Z9-16クラスター': calc_intra_distance(z9_16_genes),
    'Z10/E系列クラスター': calc_intra_distance(z10_genes)
}

# 图3: 距离热图
print("  生成图3: 距离热图...")
fig, ax = plt.subplots(figsize=(14, 12))
im = ax.imshow(df_dist.values, cmap='YlOrRd', aspect='auto')
ax.set_xticks(np.arange(len(df_dist.columns)))
ax.set_yticks(np.arange(len(df_dist.index)))
ax.set_xticklabels(df_dist.columns, rotation=90, ha='right', fontsize=8)
ax.set_yticklabels(df_dist.index, fontsize=8)

for i, label in enumerate(ax.get_xticklabels()):
    label.set_color(get_color(label.get_text()))
for i, label in enumerate(ax.get_yticklabels()):
    label.set_color(get_color(label.get_text()))

cbar = plt.colorbar(im, ax=ax)
cbar.set_label('進化距離', rotation=270, labelpad=20, fontsize=12)
ax.set_title('種間進化距離ヒートマップ', fontsize=16, fontweight='bold', pad=20)
plt.tight_layout()
output_file = os.path.join(output_dir, '図3_距離ヒートマップ.png')
plt.savefig(output_file, dpi=300, bbox_inches='tight')
plt.close()
print(f"  [OK] 保存完成")

# 图4: 分支长度分布
print("  生成图4: 分支长度分布...")
fig, ax = plt.subplots(figsize=(10, 6))
ax.hist(all_distances, bins=30, color='skyblue', edgecolor='black', alpha=0.7)
ax.axvline(np.mean(all_distances), color='red', linestyle='--', 
           linewidth=2, label=f'平均値: {np.mean(all_distances):.4f}')
ax.axvline(np.median(all_distances), color='green', linestyle='--', 
           linewidth=2, label=f'中央値: {np.median(all_distances):.4f}')
ax.set_xlabel('分岐長', fontsize=12)
ax.set_ylabel('頻度', fontsize=12)
ax.set_title('系統発生樹の分岐長分布', fontsize=14, fontweight='bold')
ax.legend(fontsize=10)
ax.grid(alpha=0.3)
plt.tight_layout()
output_file = os.path.join(output_dir, '図4_分岐長分布.png')
plt.savefig(output_file, dpi=300, bbox_inches='tight')
plt.close()
print(f"  [OK] 保存完成")

print("\n" + "=" * 60)
print("分析統計:")
print("=" * 60)
for key, value in stats.items():
    print(f"  {key}: {value:.4f}")

print("\nクラスター内平均距離:")
for cluster, dist in intra_distances.items():
    print(f"  {cluster}: {dist:.4f}")

print("\n" + "=" * 60)
print("[OK] 分析完了！すべての結果が保存されました")
print("=" * 60)
print(f"\n出力ディレクトリ: {output_dir}")

