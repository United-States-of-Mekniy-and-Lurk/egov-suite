{{- define "government-portal.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "government-portal.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{- define "government-portal.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "government-portal.selectorLabels" -}}
app.kubernetes.io/name: {{ include "government-portal.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{- define "government-portal.labels" -}}
helm.sh/chart: {{ include "government-portal.chart" . }}
{{ include "government-portal.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{- define "government-portal.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "government-portal.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

{{- define "government-portal.secretName" -}}
{{- default (printf "%s-secret" (include "government-portal.fullname" .)) .Values.secret.existingSecret }}
{{- end }}